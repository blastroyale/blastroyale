using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This is responsible for displaying the screen during spectate mode,
	/// that follows your killer around.
	/// </summary>
	public unsafe class SpectateScreenPresenter : UIPresenterData2<SpectateScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action OnLeaveClicked;
		}

		private const string USS_HIDE_CONTROLS = "hide-controls";

		private IGameServices _services;
		private IMatchServices _matchServices;

		private ScreenHeaderElement _header;
		private Label _playerName;
		private VisualElement _defeatedYou;

		// ReSharper disable NotAccessedField.Local
		private StatusBarsView _statusBarsView;
		// ReSharper reset NotAccessedField.Local

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
		}

		protected override void QueryElements()
		{
			_header = Root.Q<ScreenHeaderElement>("Header").Required();
			_playerName = Root.Q<Label>("PlayerName").Required();
			_defeatedYou = Root.Q<VisualElement>("DefeatedYou").Required();
			Root.Q("StatusBars").Required().AttachView2(this, out _statusBarsView);
			_statusBarsView.ForceOverheadUI();
			_statusBarsView.InitAll();

			_header.backClicked += Data.OnLeaveClicked;

			Root.Q<LocalizedButton>("LeaveButton").clicked += Data.OnLeaveClicked;
			Root.Q<ImageButton>("ArrowLeft").clicked += OnPreviousPlayerClicked;
			Root.Q<ImageButton>("ArrowRight").clicked += OnNextPlayerClicked;

			Root.Q<VisualElement>("ShowHide").RegisterCallback<ClickEvent, VisualElement>((_, r) =>
				r.ToggleInClassList(USS_HIDE_CONTROLS), Root);

			Root.SetupClicks(_services);
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			// TODO: Use proper localization
			var gameModeID = _services.RoomService.CurrentRoom.Properties.GameModeId.Value;
			_header.SetSubtitle(gameModeID.ToUpper());

			_matchServices.SpectateService.SpectatedPlayer.InvokeObserve(OnSpectatedPlayerChanged);

			return base.OnScreenOpen(reload);
		}

		protected override UniTask OnScreenClosed()
		{
			_matchServices.SpectateService.SpectatedPlayer.StopObservingAll(this);
			return base.OnScreenClosed();
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer _, SpectatedPlayer current)
		{
			var f = QuantumRunner.Default.Game.Frames.Predicted;
			var playersData = f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData;

			if (!current.Player.IsValid)
			{
				FLog.Warn($"Invalid player entity {current.Entity} being spectated");
				return;
			}

			if (!f.TryGet<PlayerCharacter>(current.Entity, out var _))
			{
				return;
			}

			var data = new QuantumPlayerMatchData(f, playersData[current.Player]);
			var nameColor = _services.LeaderboardService.GetRankColor(_services.LeaderboardService.Ranked, (int) data.LeaderboardRank);

			_playerName.text = data.GetPlayerName();
			_playerName.style.color = nameColor;
			_defeatedYou.SetDisplay(current.Player == _matchServices.MatchEndDataService.LocalPlayerKiller);
		}

		private void OnNextPlayerClicked()
		{
			_matchServices.SpectateService.SwipeRight();
		}

		private void OnPreviousPlayerClicked()
		{
			_matchServices.SpectateService.SwipeLeft();
		}
	}
}