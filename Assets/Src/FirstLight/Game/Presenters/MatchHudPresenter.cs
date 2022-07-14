using System.Text;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using TMPro;
using UnityEngine;
using FirstLight.Game.Logic;
using FirstLight.Game.Views.MainMenuViews;
using FirstLight.Game.Views.MatchHudViews;
using Quantum.Commands;
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
		[SerializeField, Required] private ContendersLeftHolderView _contendersLeftHolderView;
		[SerializeField, Required] private GameObject _minimapHolder;
		[SerializeField, Required] private TextMeshProUGUI _equippedDebugText;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_mapStatusText.text = "";

			foreach (var standingsButton in _standingsButtons)
			{
				standingsButton.onClick.AddListener(OnStandingsClicked);
			}

			_connectionIcon.SetActive(false);
			_standings.gameObject.SetActive(false);
			_leaderButton.onClick.AddListener(OnStandingsClicked);
			_quitButton.onClick.AddListener(OnQuitClicked);
			_services.NetworkService.HasLag.InvokeObserve(OnLag);
			_mapTimerView.gameObject.SetActive(false);
			_leaderHolderView.gameObject.SetActive(false);
			_scoreHolderView.gameObject.SetActive(false);
			_contendersLeftHolderView.gameObject.SetActive(false);

			if (SROptions.Current.EnableEquipmentDebug)
			{
				_equippedDebugText.gameObject.SetActive(true);
				QuantumEvent.Subscribe<EventOnLocalPlayerStatsChanged>(this, OnLocalPlayerStatsChanged);
			}
			else
			{
				_equippedDebugText.gameObject.SetActive(false);
			}
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_services?.NetworkService?.HasLag?.StopObservingAll(this);
		}

		protected override void OnOpened()
		{
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var isBattleRoyale = frame.Context.MapConfig.GameMode == GameMode.BattleRoyale;

			_animation.clip = _introAnimationClip;
			_animation.Play();

			_mapTimerView.gameObject.SetActive(isBattleRoyale);
			_contendersLeftHolderView.gameObject.SetActive(isBattleRoyale);
			_scoreHolderView.gameObject.SetActive(!isBattleRoyale);
			_minimapHolder.gameObject.SetActive(isBattleRoyale);

			_standings.Initialise(frame.PlayerCount, false, true);
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
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = container.GetPlayersMatchData(frame, out _);

			_standings.UpdateStandings(playerData);
			_standings.gameObject.SetActive(true);
		}

		private void OnLocalPlayerStatsChanged(EventOnLocalPlayerStatsChanged callback)
		{
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