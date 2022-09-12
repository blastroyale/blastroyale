using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using TMPro;
using UnityEngine;
using FirstLight.Game.Views.MainMenuViews;
using FirstLight.Game.Views.MatchHudViews;
using Quantum.Systems;
using Sirenix.OdinInspector;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Match HUD UI by:
	/// - Showing the Game HUD visual status
	/// </summary>		
	public class MatchHudPresenter : UiPresenter
	{
		[SerializeField, Required] private Animation _animation;
		[SerializeField, Required] private AnimationClip _introAnimationClip;
		[SerializeField, Required] private GameObject _connectionIcon;
		[SerializeField, Required] private Button _quitButton;
		[SerializeField] private Button[] _standingsButtons;
		[SerializeField, Required] private Button _leaderButton;
		[SerializeField, Required] private StandingsHolderView _standings;
		[SerializeField, Required] private TextMeshProUGUI _mapStatusText;
		[SerializeField, Required] private LeaderHolderView _leaderHolderView;
		[SerializeField, Required] private ScoreHolderView _scoreHolderView;
		[SerializeField, Required] private MapTimerView _mapTimerView;
		[SerializeField, Required] private ContendersLeftView _contendersLeftHolderView;
		[SerializeField, Required] private GameObject _minimapHolder;
		[SerializeField, Required] private TextMeshProUGUI _equippedDebugText;

		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private IMatchServices _matchServices;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_mapStatusText.text = "";

			foreach (var standingsButton in _standingsButtons)
			{
				standingsButton.onClick.AddListener(OnStandingsClicked);
			}

			_services.NetworkService.HasLag.InvokeObserve(OnLag);
			_leaderButton.onClick.AddListener(OnStandingsClicked);
			_quitButton.onClick.AddListener(OnQuitClicked);
			_equippedDebugText.gameObject.SetActive(false);
			_connectionIcon.SetActive(false);
			_standings.gameObject.SetActive(false);
			_mapTimerView.gameObject.SetActive(false);
			_leaderHolderView.gameObject.SetActive(false);
			_scoreHolderView.gameObject.SetActive(false);
			_contendersLeftHolderView.gameObject.SetActive(false);
			_quitButton.gameObject.SetActive(false);
			
			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStartedMessage);
		}

		private void OnDestroy()
		{
			QuantumEvent.UnsubscribeListener(this);
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_services?.NetworkService?.HasLag?.StopObservingAll(this);
		}

		protected override void OnOpened()
		{
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var gameModeConfig = frame.Context.GameModeConfig;

			_animation.clip = _introAnimationClip;
			_animation.Play();

			// TODO: gameModeConfig.ShowUITimer might not be enough here eventually
			_mapTimerView.gameObject.SetActive(gameModeConfig.ShowUITimer);
			_contendersLeftHolderView.gameObject.SetActive(gameModeConfig.ShowUITimer);
			_scoreHolderView.gameObject.SetActive(!gameModeConfig.ShowUITimer);
			_minimapHolder.gameObject.SetActive(gameModeConfig.ShowUIMinimap);
			_quitButton.gameObject.SetActive(true);

			_standings.Initialise(frame.PlayerCount, false, true);
		}
		
		private void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			CheckEnableQuitFunctionality();
		}
		
		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			CheckEnableQuitFunctionality();
		}

		private void CheckEnableQuitFunctionality()
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var gameContainer = frame.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			var canQuitMatch = true;
			
			if (_services.GameModeService.SelectedGameMode.Value.Entry.MatchType == MatchType.Ranked)
			{
				var localPlayer = playersData[game.GetLocalPlayers()[0]];
				var valid = localPlayer.IsValid;
				var exists = frame.Exists(localPlayer.Entity);

				canQuitMatch = !valid || !exists;
			}

			_quitButton.gameObject.SetActive(canQuitMatch);

#if DEVELOPMENT_BUILD
			if (SROptions.Current.EnableEquipmentDebug)
			{
				_equippedDebugText.gameObject.SetActive(true);
				QuantumEvent.Subscribe<EventOnPlayerEquipmentStatsChanged>(this, OnPlayerEquipmentStatsChanged);
			}
#endif
		}

		private void OnQuitClicked()
		{
			_services.MessageBrokerService.Publish(new QuitGameClickedMessage());
		}

		private void OnLag(bool previous, bool hasLag)
		{
			_connectionIcon.SetActive(hasLag);
		}

		private void OnStandingsClicked()
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = container.GetPlayersMatchData(frame, out _);
			
			_standings.UpdateStandings(playerData, game.GetLocalPlayers()[0]);
			_standings.gameObject.SetActive(true);
		}

		private void OnPlayerEquipmentStatsChanged(EventOnPlayerEquipmentStatsChanged callback)
		{
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;

			var playerCharacter = QuantumRunner.Default.Game.Frames.Verified.Get<PlayerCharacter>(callback.Entity);

			var sb = new StringBuilder();

			sb.AppendLine("Weapon:");
			AppendEquipmentDebugText(sb, playerCharacter.CurrentWeapon);

			sb.AppendLine("\nGear:");
			for (int i = 0; i < playerCharacter.Gear.Length; i++)
			{
				var gear = playerCharacter.Gear[i];
				if (gear.IsValid())
				{
					AppendEquipmentDebugText(sb, gear);
				}
			}

			_equippedDebugText.text = sb.ToString();
		}

		private static void AppendEquipmentDebugText(StringBuilder sb, Equipment equipment)
		{
			sb.AppendLine(equipment.GameId.ToString());

			sb.Append(equipment.Adjective.ToString());
			sb.Append(" Level ");
			sb.Append(equipment.Level);
			sb.Append(" ");
			sb.AppendLine(equipment.Grade.ToString());

			sb.Append(equipment.Faction.ToString());
			sb.Append(" ");
			sb.AppendLine(equipment.Rarity.ToString());
			sb.AppendLine();
		}
	}
}