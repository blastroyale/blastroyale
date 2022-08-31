using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Photon.Deterministic.Protocol;
using Photon.Deterministic.Server;
using Photon.Hive.Plugin;
using PlayFab.ServerModels;
using quantum.custom.plugin;
using ServerSDK.Modules;

namespace Quantum
{
	/// <summary>
	/// Main class to override custom quantum server behaviour.
	/// Contains all required information and overrides to change base quantum behaviour.
	/// </summary>
	public class CustomQuantumServer : DeterministicServer, IDisposable
	{
		private DeterministicSessionConfig _config;
		private RuntimeConfig _runtimeConfig;
		private readonly Dictionary<String, String> _photonConfig;
		private readonly Dictionary<string, SetPlayerData> _receivedPlayers;
		private readonly Dictionary<int, SetPlayerData> _validPlayers;
		private readonly Dictionary<int, int> _actorNrToIndex;
		
		public readonly PhotonPlayfabSDK Playfab;
		
		public CustomQuantumServer(Dictionary<String, String> photonConfig, IPluginHost host) {
			_photonConfig = photonConfig;
			Playfab = new PhotonPlayfabSDK(photonConfig, host);

			if (photonConfig.TryGetValue("TestConsensus", out var testConsensus) && testConsensus == "true")
			{
				FlgConfig.TEST_CONSENSUS = true;
			}

			_receivedPlayers = new Dictionary<string, SetPlayerData>();
			_validPlayers = new Dictionary<int, SetPlayerData>();
			_actorNrToIndex = new Dictionary<int, int>();
			ModelSerializer.RegisterConverter(new QuantumVector2Converter());
			ModelSerializer.RegisterConverter(new QuantumVector3Converter());
		}

		public int GetClientIndexByActorNumber(int actorNr) => _actorNrToIndex[actorNr];

		public int GetClientActorNumberByIndex(int index)
		{
			foreach(var actorNr in _actorNrToIndex.Keys)
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

		public string GetPlayFabId(int actorNr)
		{
			var playerRef = GetClientIndexByActorNumber(actorNr);
			Log.Debug($"Actor {actorNr} Index {playerRef} searching for playerId");
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
			Playfab.GetProfileReadOnlyData(clientPlayer.PlayerId, OnUserDataResponse);
			Log.Debug($"Received client data from player {clientPlayer.PlayerId} actor {client.ActorNr} index {clientPlayerData.Index}");
			return false; // denies adding player data to the bitstream when client sends it
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
			Log.Debug($"Validating loadout for player {playerId}");
			if (playerId == null || !_receivedPlayers.TryGetValue(playerId, out var setPlayerData))
			{
				Log.Error($"Could not find set player data request for player {playerId}");
				return;
			}
			var clientPlayer = RuntimePlayer.FromByteArray(setPlayerData.Data);
			var equipmentData = ModelSerializer.DeserializeFromData<EquipmentData>(playfabData);
			var serverHashes = equipmentData.Inventory.Values.Select(e => e.GetHashCode()).ToHashSet();

			foreach (var clientEquip in clientPlayer.Loadout)
			{
				var clientEquiphash = clientEquip.GetHashCode();
				if (!serverHashes.Contains(clientEquiphash))
				{
					Log.Error($"Player {clientPlayer.PlayerId} tried to send equipment {clientEquip.GameId} hash {clientEquiphash} which he does not own");
					return;
				}
			}
			Log.Debug($"Player {playerId} has valid loadout");
			_validPlayers[GetClientActorNumberByIndex(setPlayerData.Index)] = setPlayerData;
			SetDeterministicPlayerData(setPlayerData);
		}

		/// <summary>
		/// Called after a match ends
		/// </summary>
		public void Dispose()
		{
			_receivedPlayers.Clear();
			_validPlayers.Clear();
			_actorNrToIndex.Clear();
		}

	}
}
