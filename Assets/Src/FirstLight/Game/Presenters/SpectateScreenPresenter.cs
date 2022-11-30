using System;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Infos;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This is responsible for displaying the screen during spectate mode,
	/// that follows your killer around.
	/// </summary>
	public class SpectateScreenPresenter : UiToolkitPresenterData<SpectateScreenPresenter.StateData>
	{
		public struct StateData
		{
			public PlayerRef Killer;
			public Action OnLeaveClicked;
		}

		private const string UssHideControls = "hide-controls";

		[SerializeField] private FollowTransformView _cameraFollow;

		private IGameServices _services;
		private IMatchServices _matchServices;

		private ScreenHeaderElement _header;
		private Label _playerName;
		private MightElement _playerMight;
		private VisualElement _defeatedYou;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_header = root.Q<ScreenHeaderElement>("Header").Required();
			_playerMight = root.Q<MightElement>("PlayerMight").Required();
			_playerName = root.Q<Label>("PlayerName").Required();
			_defeatedYou = root.Q<VisualElement>("DefeatedYou").Required();

			root.Q<LocalizedButton>("Camera1").clicked += OnCamera1Clicked;
			root.Q<LocalizedButton>("Camera2").clicked += OnCamera2Clicked;
			root.Q<LocalizedButton>("Camera3").clicked += OnCamera3Clicked;
			root.Q<LocalizedButton>("LeaveButton").clicked += Data.OnLeaveClicked;
			root.Q<ImageButton>("ArrowLeft").clicked += OnPreviousPlayerClicked;
			root.Q<ImageButton>("ArrowRight").clicked += OnNextPlayerClicked;

			root.Q<VisualElement>("ShowHide")
				.RegisterCallback<ClickEvent, VisualElement>((_, r) => r.ToggleInClassList(UssHideControls), root);

			root.SetupClicks(_services);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			_header.SetSubtitle(_services.NetworkService.CurrentRoomGameModeConfig?.Id
				.ToUpper()); // TODO: Use proper localization
		}

		protected override void SubscribeToEvents()
		{
			base.SubscribeToEvents();
			_matchServices.SpectateService.SpectatedPlayer.InvokeObserve(OnSpectatedPlayerChanged);
		}

		protected override void UnsubscribeFromEvents()
		{
			base.UnsubscribeFromEvents();
			_matchServices.SpectateService.SpectatedPlayer.StopObservingAll(this);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer _, SpectatedPlayer current)
		{
			FLog.Info("PACO", $"Player switched: {current.Player}, e:{current.Entity}");

			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var playersData = f.GetSingleton<GameContainer>().PlayersData;

			var data = new QuantumPlayerMatchData(f, playersData[current.Player]);

			_cameraFollow.SetTarget(current.Transform);
			_playerName.text = data.GetPlayerName();
			_playerMight.SetMight(GetSpectatedPlayerMight(f, current), false);
			_defeatedYou.SetVisibility(current.Player == Data.Killer);
		}

		private float GetSpectatedPlayerMight(Frame f, SpectatedPlayer player)
		{
			var gear = f.Get<PlayerCharacter>(player.Entity).Gear;
			var currentWeapon = f.Get<PlayerCharacter>(player.Entity).CurrentWeapon;

			var currentEquipment = new List<Equipment>(6);
			if (currentWeapon.IsValid())
			{
				currentEquipment.Add(currentWeapon);
			}

			for (int i = 0; i < gear.Length; i++)
			{
				var item = gear[i];
				if (item.IsValid())
				{
					currentEquipment.Add(item);
				}
			}

			return currentEquipment.GetTotalMight(_services.ConfigsProvider);
		}

		private void OnNextPlayerClicked()
		{
			_matchServices.SpectateService.SwipeRight();
		}

		private void OnPreviousPlayerClicked()
		{
			_matchServices.SpectateService.SwipeLeft();
		}

		private void OnCamera1Clicked()
		{
			_services.MessageBrokerService.Publish(new SpectateSetCameraMessage {CameraId = 0});
		}

		private void OnCamera2Clicked()
		{
			_services.MessageBrokerService.Publish(new SpectateSetCameraMessage {CameraId = 1});
		}

		private void OnCamera3Clicked()
		{
			_services.MessageBrokerService.Publish(new SpectateSetCameraMessage {CameraId = 2});
		}
	}
}