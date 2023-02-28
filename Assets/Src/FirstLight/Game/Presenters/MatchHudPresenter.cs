using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstLight.Game.Data;
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
			
			_leaderButton.onClick.AddListener(OnStandingsClicked);
			_quitButton.onClick.AddListener(OnQuitClicked);
			_equippedDebugText.gameObject.SetActive(false);
			_standings.gameObject.SetActive(false);
			_leaderHolderView.gameObject.SetActive(false);
			
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

			if (_services.TutorialService.CurrentRunningTutorial.Value == TutorialSection.FIRST_GUIDE_MATCH)
			{
				_contendersLeftHolderView.gameObject.SetActive(false);
				_scoreHolderView.gameObject.SetActive(false);
			}

			_standings.Initialise(frame.PlayerCount, false, true);
		}
		
		private void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			CheckEnableQuitFunctionality(msg.Game);
		}
		
		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			CheckEnableQuitFunctionality(callback.Game);
		}

		private void CheckEnableQuitFunctionality(QuantumGame game)
		{
			var localPlayer = game.GetLocalPlayerData(true, out var f);
			var canQuitMatch = true;
			
			if (_services.NetworkService.CurrentRoomMatchType != MatchType.Custom)
			{
				var valid = localPlayer.IsValid;
				var exists = f.Exists(localPlayer.Entity);

				canQuitMatch = !valid || !exists;
			}
			else
			{
				canQuitMatch = (!_services.TutorialService.IsTutorialRunning || FeatureFlags.ALLOW_SKIP_TUTORIAL) || 
					(_services.TutorialService.IsTutorialRunning && !FeatureFlags.TUTORIAL);
			}
			
			if (_services.TutorialService.CurrentRunningTutorial.Value == TutorialSection.FIRST_GUIDE_MATCH)
			{
				canQuitMatch = false;
			}

			_quitButton.gameObject.SetActive(canQuitMatch);
		}

		private void OnQuitClicked()
		{
			_services.MessageBrokerService.Publish(new QuitGameClickedMessage());
		}

		private void OnStandingsClicked()
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = container.GeneratePlayersMatchData(frame, out _);
			
			_standings.UpdateStandings(playerData, QuantumRunner.Default.Game.GetLocalPlayers()[0]);
			_standings.gameObject.SetActive(true);
		}
	}
}