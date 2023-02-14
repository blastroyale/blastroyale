using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using Quantum;

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
		
		/// <summary>
		/// LocalPlayer at the end of the game. Will be PlayerRef.None if we're spectators
		/// </summary>
		QuantumPlayerMatchData LocalPlayerMatchData { get; }
		
		/// <summary>
		/// Player that killed the local player. Will have a value if the player was killed.
		/// </summary>
		PlayerRef LocalPlayerKiller { get; }

		/// <summary>
		/// Information about all the players that played in the match that ended.
		/// </summary>
		Dictionary<PlayerRef, PlayerMatchData> PlayerMatchData { get; }
		
		/// <summary>
		/// List of rewards
		/// </summary>
		public List<RewardData> Rewards { get; }
		
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
		/// Has local player left the match before it ended (either through menu UI, or during spectate)
		/// This data point is available before the match ends
		/// </summary>
		public bool LeftBeforeMatchFinished { get; }
	}

	public struct PlayerMatchData
	{
		public PlayerRef PlayerRef { get; }
		
		public QuantumPlayerMatchData QuantumPlayerMatchData;
		public Equipment Weapon { get; }
		public List<Equipment> Gear { get; }

		public PlayerMatchData(PlayerRef playerRef, QuantumPlayerMatchData quantumData, Equipment weapon, List<Equipment> gear)
		{
			PlayerRef = playerRef;
			QuantumPlayerMatchData = quantumData;
			Weapon = weapon;
			Gear = gear;
		}
	}
	
	/// <inheritdoc />
	public class MatchEndDataService : IMatchEndDataService, MatchServices.IMatchService
	{
		/// <inheritdoc />
		public List<QuantumPlayerMatchData> QuantumPlayerMatchData { get; private set; }

		public Dictionary<PlayerRef, EquipmentEventData> PlayersFinalEquipment { get; private set; }

		/// <inheritdoc />
		public bool ShowUIStandingsExtraInfo { get; private set; }
		/// <inheritdoc />
		public PlayerRef LocalPlayer { get; private set; }

		public QuantumPlayerMatchData LocalPlayerMatchData { get; private set; }

		/// <inheritdoc />
		public PlayerRef LocalPlayerKiller { get; private set; }

		/// <inheritdoc />
		public Dictionary<PlayerRef, PlayerMatchData> PlayerMatchData { get; private set; } = new ();
		/// <inheritdoc />
		public List<RewardData> Rewards { get; private set; }
		/// <inheritdoc />
		public int TrophiesChange { get; private set; }
		/// <inheritdoc />
		public uint TrophiesBeforeChange { get; private set; }
		/// <inheritdoc />
		public uint CSBeforeChange { get; private set; }
		/// <inheritdoc />
		public uint BPPBeforeChange { get; private set; }
		/// <inheritdoc />
		public uint BPLevelBeforeChange { get; private set; }
		/// <inheritdoc />
		public bool LeftBeforeMatchFinished { get; private set; }

		private IGameServices _services;
		private IGameDataProvider _dataProvider;

		public MatchEndDataService(IGameServices services, IGameDataProvider dataProvider)
		{
			_services = services;
			_dataProvider = dataProvider;
			PlayersFinalEquipment = new Dictionary<PlayerRef, EquipmentEventData>();
			_services.MessageBrokerService.Subscribe<LeftBeforeMatchFinishedMessage>(OnLeftBeforeMatchFinishedMessage);
		}
		
		/// <inheritdoc />
		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			TrophiesBeforeChange = _dataProvider.PlayerDataProvider.Trophies.Value;
			CSBeforeChange = (uint)_dataProvider.CurrencyDataProvider.Currencies[GameId.CS];
			ShowUIStandingsExtraInfo = game.Frames.Verified.Context.GameModeConfig.ShowUIStandingsExtraInfo;
			LocalPlayer = game.GetLocalPlayerRef();
			PlayerMatchData = new Dictionary<PlayerRef, PlayerMatchData>();
			LocalPlayerKiller = PlayerRef.None;
			PlayersFinalEquipment.Clear();

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
		}

		/// <inheritdoc />
		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
			if (isDisconnected)
			{
				return;
			}
			
			QuantumEvent.UnsubscribeListener<EventOnLocalPlayerDead>(this);
			QuantumEvent.UnsubscribeListener<EventOnPlayerKilledPlayer>(this);
			
			var frame = game.Frames.Verified;
			var gameContainer = frame.GetSingleton<GameContainer>();
			
			QuantumPlayerMatchData = gameContainer.GeneratePlayersMatchData(frame, out _);

			PlayerMatchData.Clear();
			foreach (var quantumPlayerData in QuantumPlayerMatchData)
			{
				// This means that the match disconnected before the 
				if (quantumPlayerData.Data.Player == PlayerRef.None)
				{
					return;
				}

				List<Equipment> gear = null;
				Equipment weapon = Equipment.None;
				if (PlayersFinalEquipment.ContainsKey(quantumPlayerData.Data.Player))
				{
					var equipmentData = PlayersFinalEquipment[quantumPlayerData.Data.Player];
					gear = equipmentData.Gear.ToList().FindAll(equipment => equipment.IsValid());
					weapon = equipmentData.CurrentWeapon;
				}
				else
				{
					var playerCharacter = frame.Get<PlayerCharacter>(quantumPlayerData.Data.Entity);
					gear = playerCharacter.Gear.ToList().FindAll(equipment => equipment.IsValid());
					weapon = playerCharacter.CurrentWeapon;
				}
				gear.Add(weapon);
				
				var playerData = new PlayerMatchData(quantumPlayerData.Data.Player, quantumPlayerData, weapon, gear);

				if (game.PlayerIsLocal(playerData.PlayerRef))
				{
					LocalPlayerMatchData = quantumPlayerData;
				}
				
				PlayerMatchData.Add(playerData.PlayerRef, playerData);
			}

			GetRewards(frame, gameContainer);
		}
		
		private void OnLeftBeforeMatchFinishedMessage(LeftBeforeMatchFinishedMessage msg)
		{
			LeftBeforeMatchFinished = true;
		}

		private void GetRewards(Frame frame, GameContainer gameContainer)
		{
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			var matchType = room?.GetMatchType() ?? _services.GameModeService.SelectedGameMode.Value.Entry.MatchType;

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
				ExecutingPlayer = LocalPlayer,
				MatchType = matchType,
				DidPlayerQuit = false,
				GamePlayerCount = QuantumPlayerMatchData.Count()
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