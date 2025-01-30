using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using FirstLight.Services;
using Quantum;
using UnityEngine;
using UnityEngine.Rendering.LookDev;
using IDataProvider = FirstLight.Server.SDK.Models.IDataProvider;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Service that holds all the data from the match to be used once the simulation is over.
	/// </summary>
	public interface IMatchEndDataService
	{
		// TODO: Remove this property once all the match end screens are redone and use PlayerMatchData instead
		/// <summary>
		/// List of all the QuantumPlayerData at the end of the game. Used in the places that need the frame.GetSingleton<GameContainer>().GeneratePlayersMatchData
		/// </summary>
		List<QuantumPlayerMatchData> QuantumPlayerMatchData { get; }

		/// <summary>
		/// LocalPlayer at the end of the game. Will be PlayerRef.None if we're spectators
		/// </summary>
		PlayerRef LocalPlayer { get; }

		PlayerRef Leader { get; }

		/// <summary>
		/// LocalPlayer at the end of the game. Will be PlayerRef.None if we're spectators
		/// </summary>
		QuantumPlayerMatchData LocalPlayerMatchData { get; }

		/// <summary>
		/// MatchConfigs used during this match.
		/// </summary>
		SimulationMatchConfig MatchConfig { get; }
		
		/// <summary>
		/// Player that killed the local player. Will have a value if the player was killed.
		/// </summary>
		PlayerRef LocalPlayerKiller { get; }

		/// <summary>
		/// If the player was killed by standing on a roof and taking damage.
		/// </summary>
		bool DiedFromRoofDamage { get; }

		/// <summary>
		/// Information about all the players that played in the match that ended.
		/// </summary>
		Dictionary<PlayerRef, ClientCachedPlayerMatchData> PlayerMatchData { get; }

		/// <summary>
		/// Has local player left the match before it ended (either through menu UI, or during spectate)
		/// This data point is available before the match ends
		/// </summary>
		public bool LeftBeforeMatchFinished { get; }

		/// <summary>
		/// In-Memory cache of player rewards and states to be used in reward screen
		/// </summary>
		public RewardDataCache CachedRewards { get; }

		/// <summary>
		/// Read all data from simulation. Just in case we missed something for a reconnecting player
		/// </summary>
		void Reload();
		
		/// <summary>
		/// Checks if current player played the game as spectator
		/// </summary>
		bool JoinedAsSpectator { get; }
	}

	/// <summary>
	/// Cached match data after simulation to be read by ending sequence
	/// </summary>
	public class ClientCachedPlayerMatchData
	{
		public PlayerRef PlayerRef { get; }
		public QuantumPlayerMatchData QuantumPlayerMatchData;
		public Equipment Weapon { get; }
		public GameId[] Cosmetics { get; }

		public ClientCachedPlayerMatchData(PlayerRef playerRef, QuantumPlayerMatchData quantumData, Equipment weapon,
										   GameId[] cosmetics)
		{
			PlayerRef = playerRef;
			QuantumPlayerMatchData = quantumData;
			Weapon = weapon;
			Cosmetics = cosmetics;
		}
	}

	public class RewardDataCache
	{
		public PlayerData Before = new ();
		public Tuple<uint, uint> BattlePassBefore = new (0, 0);
		public MatchRewardsResult ReceivedInCommand { get; set; } = new ();
	}

	public class MatchEndDataService : IMatchEndDataService, IMatchService
	{
		public List<QuantumPlayerMatchData> QuantumPlayerMatchData { get; private set; }
		public bool LeftBeforeMatchFinished { get; set; }
		public RewardDataCache CachedRewards { get; private set; }
		public PlayerRef Leader { get; private set; }
		public bool JoinedAsSpectator { get; private set; }
		public Dictionary<PlayerRef, EquipmentEventData> PlayersFinalEquipment { get; private set; }
		public bool ShowUIStandingsExtraInfo { get; private set; }
		public PlayerRef LocalPlayer { get; private set; }
		public QuantumPlayerMatchData LocalPlayerMatchData { get; private set; }
		public SimulationMatchConfig MatchConfig { get; private set; }
		public PlayerRef LocalPlayerKiller { get; private set; }
		public bool DiedFromRoofDamage { get; private set; }
		public Dictionary<PlayerRef, ClientCachedPlayerMatchData> PlayerMatchData { get; private set; } = new ();
		
		public void Reload()
		{
			ReadMatchDataForEndingScreens(QuantumRunner.Default.Game);
		}

		private IGameServices _services;
		private IGameDataProvider _dataProvider;

		public MatchEndDataService(IGameServices services, IGameDataProvider dataProvider)
		{
			_services = services;
			_dataProvider = dataProvider;
			PlayersFinalEquipment = new Dictionary<PlayerRef, EquipmentEventData>();
			_services.MessageBrokerService.Subscribe<LeftBeforeMatchFinishedMessage>(OnLeftBeforeMatchFinishedMessage);
			_services.MessageBrokerService.Subscribe<BeforeSimulationCommand>(OnBeforeSimulationCommand);
			_services.MessageBrokerService.Subscribe<GameCompletedRewardsMessage>(OnGameRewards);
		}

		private void ReadInitialValues(QuantumGame game)
		{
			ShowUIStandingsExtraInfo = game.Frames.Verified.Context.GameModeConfig.ShowUIStandingsExtraInfo;
			LocalPlayer = game.GetLocalPlayerRef();
			PlayerMatchData = new Dictionary<PlayerRef, ClientCachedPlayerMatchData>();
			LocalPlayerKiller = PlayerRef.None;
			PlayersFinalEquipment.Clear();
		}

		private PlayerData CopyPlayerData()
		{
			var pd = _services.DataService.GetData<PlayerData>();
			var serialized = ModelSerializer.Serialize(pd);
			return ModelSerializer.Deserialize<PlayerData>(serialized.Value);
		}

		private void OnGameRewards(GameCompletedRewardsMessage msg)
		{
			CachedRewards.ReceivedInCommand = msg.Rewards;
		}

		private void OnBeforeSimulationCommand(BeforeSimulationCommand msg)
		{
			if (msg.Type != QuantumServerCommand.EndOfGameRewards) return;
			CachedRewards = new RewardDataCache();
			CachedRewards.Before = CopyPlayerData();
			CachedRewards.BattlePassBefore = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();
		}

		/// <inheritdoc />
		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			ReadInitialValues(game);

			QuantumEvent.SubscribeManual<EventOnPlayerDead>(this, OnPlayerDead);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
		}

		private void OnPlayerDead(EventOnPlayerDead callback)
		{
			PlayersFinalEquipment[callback.Player] = callback.EquipmentData;
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			LocalPlayerMatchData =
				callback.PlayersMatchData.Find(data => callback.Game.PlayerIsLocal(data.Data.Player));
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			LocalPlayerKiller = callback.PlayerKiller;
			DiedFromRoofDamage = callback.FromRoofDamage;
		}

		public unsafe void ReadMatchDataForEndingScreens(QuantumGame game)
		{
			FLog.Verbose("Reading simulation data for match end sequence");
			if (game == null || game.Frames.Verified == null || game.IsSessionDestroyed)
			{
				FLog.Error("Simulation was null or game was destroyed, could not read simulation data");
				return;
			}

			var frame = game.Frames.Verified;
			if (!frame.TryGetSingletonEntityRef<GameContainer>(out var containerEntity))
			{
				FLog.Error("Trying to read simulation data without a game container in memory");
				return;
			}

			if (!frame.Unsafe.TryGetPointerSingleton<GameContainer>(out var gameContainer))
			{
				FLog.Error("Trying to read simulation data without a game container in memory");
				return;
			}
			
			LocalPlayer = game.GetLocalPlayerRef();
			QuantumPlayerMatchData = gameContainer->GeneratePlayersMatchData(frame, out var leader, out _);
			JoinedAsSpectator = _services.RoomService.IsLocalPlayerSpectator;
			Leader = leader;
			PlayerMatchData.Clear();
			MatchConfig = game.Configurations.Runtime.MatchConfigs;
			foreach (var quantumPlayerData in QuantumPlayerMatchData)
			{
				// This means that the match disconnected before the 
				if (quantumPlayerData.Data.Player == PlayerRef.None)
				{
					continue;
				}

				var frameData = frame.GetPlayerData(quantumPlayerData.Data.Player);
				
				Equipment weapon = Equipment.None;

				if (PlayersFinalEquipment.ContainsKey(quantumPlayerData.Data.Player))
				{
					var equipmentData = PlayersFinalEquipment[quantumPlayerData.Data.Player];
					weapon = equipmentData.CurrentWeapon;
				}
				else if (frame.Has<PlayerCharacter>(quantumPlayerData.Data.Entity))
				{
					var playerCharacter = frame.Get<PlayerCharacter>(quantumPlayerData.Data.Entity);
					weapon = playerCharacter.CurrentWeapon;
				}

				var cosmetics = PlayerLoadout.GetCosmetics(frame, quantumPlayerData.Data.Entity);
				
				if (!quantumPlayerData.IsBot && cosmetics.Length == 0 && frameData != null)
				{
					cosmetics = frameData.Cosmetics;
				}
				else if (cosmetics.Length == 0 && PlayersFinalEquipment.TryGetValue(quantumPlayerData.Data.Player, out var equipmentEventData))
				{
					cosmetics = new [] { equipmentEventData.Skin };
				}
				
				var playerData = new ClientCachedPlayerMatchData(quantumPlayerData.Data.Player, quantumPlayerData, weapon, cosmetics);

				if (game.PlayerIsLocal(playerData.PlayerRef))
				{
					LocalPlayerMatchData = quantumPlayerData;
				}

				PlayerMatchData.Add(playerData.PlayerRef, playerData);
			}
		}

		/// <inheritdoc />
		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
			QuantumEvent.UnsubscribeListener<EventOnLocalPlayerDead>(this);
			QuantumEvent.UnsubscribeListener<EventOnPlayerKilledPlayer>(this);

			if (isDisconnected)
			{
				ReadInitialValues(game);
			}

			ReadMatchDataForEndingScreens(game);
		}

		private void OnLeftBeforeMatchFinishedMessage(LeftBeforeMatchFinishedMessage msg)
		{
			LeftBeforeMatchFinished = true;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_services.MessageBrokerService?.UnsubscribeAll(this);
		}
	}
}