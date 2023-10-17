using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Serializers;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using Photon.Deterministic;
using Photon.Deterministic.Protocol;
using Photon.Deterministic.Server;
using Photon.Hive.Plugin;
using PlayFab.ServerModels;
using quantum.custom.plugin;

namespace Quantum
{
    /// <summary>
    /// Main class to override custom quantum server behaviour.
    /// Contains all required information and overrides to change base quantum behaviour.
    /// </summary>
    public class CustomQuantumServer : DeterministicServer, IDisposable
    {
        private static ResourceManagerStaticPreloaded _resourceManager;
        private static QuantumAssetSerializer _serializer = new QuantumAssetSerializer();
        private static Object _initializationLock = new Object();
        
        private DeterministicSessionConfig _config;
        private RuntimeConfig _runtimeConfig;
        private readonly Dictionary<String, String> _photonConfig;
        private readonly Dictionary<string, SetPlayerData> _receivedPlayers;
        private readonly Dictionary<int, SetPlayerData> _validPlayers;
        private readonly Dictionary<int, int> _actorNrToIndex;
        public SessionContainer gameSession;
        private InputProvider inputProvider;
        private bool _serverSimulation = true;

        public readonly PhotonPlayfabSDK Playfab;
        public Action<EventFireQuantumServerCommand> OnSimulationCommand;

        public CustomQuantumServer(Dictionary<String, String> photonConfig, IPluginHost host)
        {
            _photonConfig = photonConfig;
            Playfab = new PhotonPlayfabSDK(photonConfig, host);
            _receivedPlayers = new Dictionary<string, SetPlayerData>();
            _validPlayers = new Dictionary<int, SetPlayerData>();
            _actorNrToIndex = new Dictionary<int, int>();
            FlgCustomSerializers.RegisterSerializers();
            if (photonConfig.TryGetValue("simulation", out var runSim) && runSim == "false")
            {
                _serverSimulation = false;
            }
        }

        #region Server Simulation

        public override void OnDeterministicStartSession()
        {
            if (!_serverSimulation)
            {
                return;
            }

            lock (_initializationLock)
            {
                if (!FPLut.IsLoaded)
                {
                    FPLut.Init(Path.Combine(PluginLocation, "MathTables"));
                    String pathToDB = Path.Combine(PluginLocation, "assetDatabase.json");
                    byte[] assetDBData = LoadAssetDBData(pathToDB, null);
                    Assert.Always(assetDBData != null, "No asset database found");
                    Native.Utils = Native.Utils ?? SessionContainer.CreateNativeUtils();
                    var assets = _serializer.DeserializeAssets(assetDBData);
                    var allocator = SessionContainer.CreateNativeAllocator();
                    _resourceManager = ResourceManagerStaticPreloaded.Create(assets, allocator);
                }
            }

            StartServerSimulation();
        }

        /// <summary>
        /// Called whenever the simulation fires a command that should be directed to the server
        /// This would transform the event into a logic server command.
        private void OnServerCommand(EventFireQuantumServerCommand ev)
        {
            if (!_serverSimulation)
            {
                return;
            }

            if (FlgConfig.DebugMode)
            {
                Log.Info($"Received server command {ev.CommandType} from player {ev.Player}");
            }

            OnSimulationCommand?.Invoke(ev);
        }

        public void StartServerSimulation()
        {
            if (!_serverSimulation)
            {
                return;
            }

            var events = new EventDispatcher();
            events.Subscribe<EventFireQuantumServerCommand>(this, OnServerCommand);
            var configsFile = new ReplayFile();
            configsFile.DeterministicConfig = _config;
            configsFile.RuntimeConfig = _runtimeConfig;
            gameSession = new SessionContainer(configsFile);
            var startParams = new QuantumGame.StartParameters
            {
                AssetSerializer = _serializer,
                ResourceManager = _resourceManager,
                EventDispatcher = events,
            };
            inputProvider = new InputProvider(_config);
            var taskRunner = new InactiveTaskRunner();
            gameSession.StartReplay(startParams, inputProvider, "server", false, taskRunner: taskRunner);
        }

        public string PluginLocation
        {
            get
            {
                string codeBase = GetType().Assembly.CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        private byte[] LoadAssetDBData(string pathToDB, string embeddedDB)
        {
            byte[] assetDBFileContent = null;

            // Trying to load the asset db file from disk
            if (string.IsNullOrEmpty(pathToDB) == false)
            {
                if (File.Exists(pathToDB))
                {
                    PluginHost.LogInfo($"Loading Quantum AssetDB from file '{pathToDB}' ..");
                    assetDBFileContent = File.ReadAllBytes(pathToDB);
                    Assert.Always(assetDBFileContent != null);
                }
                else
                {
                    PluginHost.LogInfo($"No asset db file found at '{pathToDB}'.");
                }
            }

            // Trying to load the asset db file from the assembly
            if (assetDBFileContent == null)
            {
                PluginHost.LogInfo($"Loading Quantum AssetDB from internal resource '{embeddedDB}'");
                using (var stream = typeof(QuantumGame).Assembly.GetManifestResourceStream(embeddedDB))
                {
                    if (stream != null)
                    {
                        if (stream.Length > 0)
                        {
                            assetDBFileContent = new byte[stream.Length];
                            var bytesRead = stream.Read(assetDBFileContent, 0, (int)stream.Length);
                            Assert.Always(bytesRead == (int)stream.Length);
                        }
                        else
                        {
                            PluginHost.LogError($"The file '{embeddedDB}' in assembly '{typeof(QuantumGame).Assembly.FullName}' is empty.");
                        }
                    }
                    else
                    {
                        PluginHost.LogError($"Failed to find the Quantum AssetDB resource from '{embeddedDB}' in assembly '{typeof(QuantumGame).Assembly.FullName}'. Here are all resources found inside the assembly:");
                        foreach (var name in typeof(QuantumGame).Assembly.GetManifestResourceNames())
                        {
                            PluginHost.LogInfo(name);
                        }
                    }
                }
            }

            return assetDBFileContent;
        }

		/// <summary>
		/// Method responsible for receiving client inputs and forwarding them to the server simulation
		/// </summary>
		public override void OnDeterministicInputConfirmed(DeterministicPluginClient client, int tick, int playerIndex, DeterministicTickInput input)
		{
			if (!_serverSimulation)
			{
				return;
			}
			inputProvider.InjectInput(input, true);
		}

        /// <summary>
        /// Method called when ticking the simulation.
        /// Will advance the simulation necessary frames.
        /// </summary>
        public override void OnDeterministicUpdate()
        {
            if (!_serverSimulation)
            {
                return;
            }

            if (gameSession == null)
            {
                return;
            }

            try
            {
                if (gameSession.Session.FrameVerified != null)
                {
                    // Interpolate time to make sure server catchup if it ticked too slow for some reason
                    double gameTime = Session.Input.GameTime;
                    var sessionTime = gameSession.Session.AccumulatedTime + gameSession.Session.FrameVerified.Number * gameSession.Session.DeltaTimeDouble;
                    gameSession.Service(gameTime - sessionTime);
                }
                else
                {
                    gameSession.Service();
                }
            }
            catch (Exception e)
            {
                PluginHost.LogError($"An exception was thrown while servicing the GameSession.");
                PluginHost.LogException(e);

                gameSession?.Destroy();
                gameSession = null;
            }
        }

        /// <summary>
        /// Called when clients requests a snapshot of the last validated frame.
        /// Only works for validated frames server-side. Server does not run predicted frames.
        /// </summary>
        public override Boolean OnDeterministicSnapshotRequested(ref Int32 tick, ref byte[] data)
        {
            if (!_serverSimulation)
            {
                return false;
            }

            if (gameSession?.Session?.FrameVerified == null)
            {
                return false;
            }

            tick = gameSession.Session.FrameVerified.Number;
            data = gameSession.Session.FrameVerified.Serialize(DeterministicFrameSerializeMode.Serialize);
            return true;
        }

        #endregion

        public int GetClientIndexByActorNumber(int actorNr) => _actorNrToIndex[actorNr];

        /// <summary>
        /// Obtains the actor number based on the quantum index
        /// </summary>
        public int GetClientActorNumberByIndex(int index)
        {
            foreach (var actorNr in _actorNrToIndex.Keys)
            {
                if (_actorNrToIndex[actorNr] == index)
                    return actorNr;
            }

            return -1;
        }

        public Dictionary<int, SetPlayerData> GetValidatedPlayers() => _validPlayers;

        /// <summary>
        /// Called whenever a game session is about to start and client passes down session configuration.
        /// </summary>
        public override void OnDeterministicSessionConfig(DeterministicPluginClient client, SessionConfig configData)
        {
            _config = configData.Config;
        }

        public override void OnDeterministicRuntimeConfig(DeterministicPluginClient client, Photon.Deterministic.Protocol.RuntimeConfig configData)
        {
            base.OnDeterministicRuntimeConfig(client, configData);
            _runtimeConfig = RuntimeConfig.FromByteArray(configData.Config);
        }

        public string GetPlayFabIdByIndex(int playerRef)
        {
            if (FlgConfig.DebugMode)
            {
                Log.Debug($"Actor Index {playerRef} searching for playerId");
            }

            foreach (var playfabId in _receivedPlayers.Keys)
            {
                if (_receivedPlayers[playfabId].Index == playerRef)
                    return playfabId;
            }

            return null;
        }

        public string GetPlayFabId(int actorNr)
        {
            var playerRef = GetClientIndexByActorNumber(actorNr);
            if (FlgConfig.DebugMode)
            {
                Log.Debug($"Actor {actorNr} Index {playerRef} searching for playerId");
            }

            foreach (var playfabId in _receivedPlayers.Keys)
            {
                if (_receivedPlayers[playfabId].Index == playerRef)
                    return playfabId;
            }

            return null;
        }

        /// <summary>
        /// Override method that will block adding any player data from to the relay stream by direct client input.
        /// Will call external services via HTTP to validate if the client input is correct, and only after
        /// verification it will add the RuntimePlayer serialized object to relay BitStream.
        /// </summary>
        public override bool OnDeterministicPlayerDataSet(DeterministicPluginClient client, SetPlayerData clientPlayerData)
        {
            if (_validPlayers.ContainsKey(client.ActorNr))
                return true;

            var clientPlayer = RuntimePlayer.FromByteArray(clientPlayerData.Data);
            _receivedPlayers[clientPlayer.PlayerId] = clientPlayerData;
            _actorNrToIndex[client.ActorNr] = clientPlayerData.Index;
            
            // Remote playfab validation disabled until we minimize serialization/data traffic usage
            // to ensure scale
            //Playfab.GetProfileReadOnlyData(clientPlayer.PlayerId, OnUserDataResponse);

            if (FlgConfig.DebugMode)
            {
                Log.Info($"Received client data from player {clientPlayer.PlayerId} actor {client.ActorNr} index {clientPlayerData.Index}");
            }

            return MinimalAntiHackValidation(clientPlayer); 
        }

        /// <summary>
        /// Minimal "localhost" validation that players are not sending silly things to the match.
        /// This is just needed while remote gear validation is disabled
        /// </summary>
        private bool MinimalAntiHackValidation(RuntimePlayer player)
        {
            foreach (var cosmetic in player.Cosmetics)
            {
                if (!cosmetic.IsInGroup(GameIdGroup.Collection))
                {
                    Log.Error($"Player {player.PlayerId} sent invalid cosmetic id {cosmetic}");
                    return false;
                }
            }

            foreach (var eq in player.Loadout)
            {
                if (!eq.GameId.IsInGroup(GameIdGroup.Equipment))
                {
                    Log.Error($"Player {player.PlayerId} sent invalid equipment id {eq.GameId}");
                    return false;
                }
            }

            if (!player.Weapon.IsWeapon() && !player.Weapon.IsDefaultItem())
            {
                Log.Error($"Player {player.PlayerId} sent invalid weapon id {player.Weapon.GameId}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Callback for receiving player data from playfab.
        /// Will match the data sent from client 
        /// </summary>
        private void OnUserDataResponse(IHttpResponse response, object userState)
        {
            var playfabResponse = Playfab.HttpWrapper.DeserializePlayFabResponse<GetUserDataResult>(response);
            var playfabData = playfabResponse.Data.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.Value);
            var playerId = response.Request.UserState as string;
            if (FlgConfig.DebugMode)
            {
                Log.Debug($"Validating loadout for player {playerId}");
            }

            if (playerId == null || !_receivedPlayers.TryGetValue(playerId, out var setPlayerData))
            {
                Log.Error($"Could not find set player data request for player {playerId}");
                return;
            }

            var clientPlayer = RuntimePlayer.FromByteArray(setPlayerData.Data);
            var equipmentData = ModelSerializer.DeserializeFromData<EquipmentData>(playfabData);
            var validItemHashes = new HashSet<int>();

            foreach (var itemTuple in equipmentData.Inventory)
            {
                var isNft = equipmentData.NftInventory.ContainsKey(itemTuple.Key);
                if (isNft || !itemTuple.Value.IsBroken())
                {
                    validItemHashes.Add(itemTuple.Value.GetServerHashCode());
                }
            }

            foreach (var clientEquip in clientPlayer.Loadout)
            {
                var clientEquiphash = clientEquip.GetServerHashCode();
                if (!validItemHashes.Contains(clientEquiphash))
                {
                    Log.Error($"Player {clientPlayer.PlayerId} tried to send equipment {clientEquip.GameId} hash {clientEquiphash} which he does not own or cant be used atm");
                    return;
                }
            }

            if (!ValidatePlayerCosmetics(playfabData, ref clientPlayer))
            {
                Log.Error(
                    $"Player {clientPlayer.PlayerId} tried to send cosmetics {string.Join(",", clientPlayer.Cosmetics.Select(s => s.ToString()))} which he doesn't have!");
                return;
            }

            if (FlgConfig.DebugMode)
            {
                Log.Debug($"Player {playerId} has valid loadout");
            }

            _validPlayers[GetClientActorNumberByIndex(setPlayerData.Index)] = setPlayerData;
            SetDeterministicPlayerData(setPlayerData);
        }

        public bool ValidatePlayerCosmetics(Dictionary<string, string> playfabData, ref RuntimePlayer playerData)
        {
            var skins = playerData.Cosmetics;
            var collectionData = ModelSerializer.DeserializeFromData<CollectionData>(playfabData);
            return skins.All(skin => collectionData.HasCollectionItem(skin));
        }

        /// <summary>
        /// Called after a match ends
        /// </summary>
        public void Dispose()
        {
            if (FlgConfig.DebugMode)
            {
                Log.Debug("Destroying simulation");
            }

            gameSession?.Destroy();
            _receivedPlayers.Clear();
            _validPlayers.Clear();
            _actorNrToIndex.Clear();
        }
    }
}