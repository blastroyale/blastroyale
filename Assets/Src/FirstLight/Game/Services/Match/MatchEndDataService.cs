using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

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

		Dictionary<PlayerRef, EquipmentEventData> PlayersFinalEquipment { get; }

		/// <summary>
		/// Config value used to know if the match end leaderboard should show the extra info
		/// </summary>
		bool ShowUIStandingsExtraInfo { get; }

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
		Dictionary<PlayerRef, PlayerMatchData> PlayerMatchData { get; }

		/// <summary>
		/// List of rewards
		/// </summary>
		public List<ItemData> Rewards { get; }

		/// <summary>
		/// How trophies total changed
		/// </summary>
		public int TrophiesChange { get; }

		/// <summary>
		/// How many trophies player had before change
		/// </summary>
		public uint TrophiesBeforeChange { get; }

		/// <summary>
		/// How much CS the player had before the change
		/// </summary>
		public uint CSBeforeChange { get; }

		/// <summary>
		/// How much BPP the player had before the change
		/// </summary>
		public uint BPPBeforeChange { get; }

		/// <summary>
		/// What level was the player in BP before the change
		/// </summary>
		public uint BPLevelBeforeChange { get; }
		
		/// <summary>
		/// How much BPP the player had before the change
		/// </summary>
		public uint LevelBeforeChange { get; }

		/// <summary>
		/// What level was the player in BP before the change
		/// </summary>
		public uint XPBeforeChange { get; }

		/// <summary>
		/// Has local player left the match before it ended (either through menu UI, or during spectate)
		/// This data point is available before the match ends
		/// </summary>
		public bool LeftBeforeMatchFinished { get; }

		/// <summary>
		/// Read all data from simulation. Just in case we missed something for a reconnecting player
		/// </summary>
		void Reload();
	}

	public struct PlayerMatchData
	{
		public PlayerRef PlayerRef { get; }

		public QuantumPlayerMatchData QuantumPlayerMatchData;
		public Equipment Weapon { get; }
		public List<Equipment> Gear { get; }

		public PlayerMatchData(PlayerRef playerRef, QuantumPlayerMatchData quantumData, Equipment weapon,
							   List<Equipment> gear)
		{
			PlayerRef = playerRef;
			QuantumPlayerMatchData = quantumData;
			Weapon = weapon;
			Gear = gear;
		}
	}

	public class MatchEndDataService : IMatchEndDataService, MatchServices.IMatchService
	{
		public List<QuantumPlayerMatchData> QuantumPlayerMatchData { get; private set; }

		public PlayerRef Leader { get; private set; }
		
		public Dictionary<PlayerRef, EquipmentEventData> PlayersFinalEquipment { get; private set; }
		public bool ShowUIStandingsExtraInfo { get; private set; }
		public PlayerRef LocalPlayer { get; private set; }
		public QuantumPlayerMatchData LocalPlayerMatchData { get; private set; }
		public PlayerRef LocalPlayerKiller { get; private set; }
		public bool DiedFromRoofDamage { get; private set; }
		public Dictionary<PlayerRef, PlayerMatchData> PlayerMatchData { get; private set; } = new();
		public List<ItemData> Rewards { get; private set; }
		public int TrophiesChange { get; private set; }
		public uint TrophiesBeforeChange { get; private set; }
		public uint CSBeforeChange { get; private set; }
		public uint BPPBeforeChange { get; private set; }
		public uint BPLevelBeforeChange { get; private set; }
		public uint LevelBeforeChange { get; private set;}
		public uint XPBeforeChange { get; private set;}
		public bool LeftBeforeMatchFinished { get; private set; }

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
		}

		private void ReadInitialValues(QuantumGame game)
		{
			TrophiesBeforeChange = _dataProvider.PlayerDataProvider.Trophies.Value;
			CSBeforeChange = (uint) _dataProvider.CurrencyDataProvider.Currencies[GameId.CS];
			ShowUIStandingsExtraInfo = game.Frames.Verified.Context.GameModeConfig.ShowUIStandingsExtraInfo;
			LocalPlayer = game.GetLocalPlayerRef();
			PlayerMatchData = new Dictionary<PlayerRef, PlayerMatchData>();
			LocalPlayerKiller = PlayerRef.None;
			PlayersFinalEquipment.Clear();
			LevelBeforeChange = _dataProvider.PlayerDataProvider.Level.Value;
			XPBeforeChange = _dataProvider.PlayerDataProvider.XP.Value;
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

		public void ReadMatchDataForEndingScreens(QuantumGame game)
		{
			var frame = game.Frames.Verified;
			var gameContainer = frame.GetSingleton<GameContainer>();
			LocalPlayer = game.GetLocalPlayerRef();

			QuantumPlayerMatchData = gameContainer.GeneratePlayersMatchData(frame, out var leader, out _);

			Leader = leader;

			PlayerMatchData.Clear();
			foreach (var quantumPlayerData in QuantumPlayerMatchData)
			{
				// This means that the match disconnected before the 
				if (quantumPlayerData.Data.Player == PlayerRef.None)
				{
					continue;
				}

				var frameData = frame.GetPlayerData(quantumPlayerData.Data.Player);

				List<Equipment> gear = new();
				Equipment weapon = Equipment.None;
				if (PlayersFinalEquipment.ContainsKey(quantumPlayerData.Data.Player))
				{
					var equipmentData = PlayersFinalEquipment[quantumPlayerData.Data.Player];
					gear = equipmentData.Gear.ToList().FindAll(equipment => equipment.IsValid());
					weapon = equipmentData.CurrentWeapon;
				}
				else if (frame.Has<PlayerCharacter>(quantumPlayerData.Data.Entity))
				{
					var playerCharacter = frame.Get<PlayerCharacter>(quantumPlayerData.Data.Entity);
					gear = playerCharacter.Gear.ToList().FindAll(equipment => equipment.IsValid());
					weapon = playerCharacter.CurrentWeapon;
				}
				else if (frameData != null)
				{
					weapon = frameData.Weapon;
					gear = frameData.Loadout.Where(l => l.IsValid()).ToList();
				}

				if (weapon.IsValid())
				{
					gear.Add(weapon);
				}

				var playerData = new PlayerMatchData(quantumPlayerData.Data.Player, quantumPlayerData, weapon, gear);

				if (game.PlayerIsLocal(playerData.PlayerRef))
				{
					LocalPlayerMatchData = quantumPlayerData;
				}

				PlayerMatchData.Add(playerData.PlayerRef, playerData);
			}

			GetRewards(frame, gameContainer);
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

		private void GetRewards(Frame frame, GameContainer gameContainer)
		{
			var playerRef = LocalPlayer == PlayerRef.None
				? Leader
				: LocalPlayer;
			
			var room = _services.RoomService.CurrentRoom;
			var matchType = room?.Properties.MatchType.Value ?? _services.GameModeService.SelectedGameMode.Value.Entry.MatchType;

			if (!frame.Context.GameModeConfig.AllowEarlyRewards && !gameContainer.IsGameCompleted &&
				!gameContainer.IsGameOver)
			{
				return;
			}

			var predictedProgress = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();
			BPPBeforeChange = predictedProgress.Item2;
			BPLevelBeforeChange = predictedProgress.Item1;

			var rewardSource = new RewardSource()
			{
				MatchData = QuantumPlayerMatchData,
				ExecutingPlayer = playerRef,
				MatchType = matchType,
				DidPlayerQuit = false,
				GamePlayerCount = QuantumPlayerMatchData.Count(),
				AllowedRewards = _services.RoomService.CurrentRoom.Properties.AllowedRewards.Value
			};
			Rewards = _dataProvider.RewardDataProvider.CalculateMatchRewards(rewardSource, out var trophyChange);
			TrophiesChange = trophyChange;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_services.MessageBrokerService?.UnsubscribeAll(this);
		}
	}
}