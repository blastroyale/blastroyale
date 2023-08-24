using System;
using System.Collections.Generic;
using Cinemachine;
using FirstLight.Game.Infos;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
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
			public Action OnLeaveClicked;
		}

		private const string UssHideControls = "hide-controls";

		[SerializeField] private CinemachineVirtualCamera _followCamera;

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

			_header.homeClicked += Data.OnLeaveClicked;

			root.Q<LocalizedButton>("LeaveButton").clicked += Data.OnLeaveClicked;
			root.Q<ImageButton>("ArrowLeft").clicked += OnPreviousPlayerClicked;
			root.Q<ImageButton>("ArrowRight").clicked += OnNextPlayerClicked;

			root.Q<VisualElement>("ShowHide").RegisterCallback<ClickEvent, VisualElement>((_, r) =>
				r.ToggleInClassList(UssHideControls), root);

			root.SetupClicks(_services);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			// TODO: Use proper localization
			_header.SetSubtitle(_services.NetworkService.CurrentRoomGameModeConfig?.Id.ToUpper());
		}

		protected override void SubscribeToEvents()
		{
			base.SubscribeToEvents();
			_matchServices.SpectateService.SpectatedPlayer.InvokeObserve(OnSpectatedPlayerChanged);
			QuantumEvent.Subscribe<EventOnPlayerEquipmentStatsChanged>(this, OnPlayerEquipmentStatsChanged);
		}

		protected override void UnsubscribeFromEvents()
		{
			base.UnsubscribeFromEvents();
			_matchServices.SpectateService.SpectatedPlayer.StopObservingAll(this);
		}

		private void OnPlayerEquipmentStatsChanged(EventOnPlayerEquipmentStatsChanged callback)
		{
			if (callback.Player != _matchServices.SpectateService.SpectatedPlayer.Value.Player) return;

			var f = QuantumRunner.Default.Game.Frames.Predicted;
			if (f.TryGet<PlayerCharacter>(callback.Entity, out var playerCharacter))
			{
				UpdateCurrentMight(playerCharacter);
			}
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer _, SpectatedPlayer current)
		{
			var f = QuantumRunner.Default.Game.Frames.Predicted;
			var playersData = f.GetSingleton<GameContainer>().PlayersData;

			if (!f.TryGet<PlayerCharacter>(current.Entity, out var playerCharacter))
			{
				return;
			}

			var data = new QuantumPlayerMatchData(f, playersData[current.Player]);
			
			_followCamera.Follow = current.Transform;
			_followCamera.LookAt = current.Transform;
			_followCamera.SnapCamera();
			
			_playerName.text = data.GetPlayerName();
			_defeatedYou.SetVisibility(current.Player == _matchServices.MatchEndDataService.LocalPlayerKiller);
			UpdateCurrentMight(playerCharacter);
		}

		private void UpdateCurrentMight(PlayerCharacter character)
		{
			_playerMight.SetMight(GetSpectatedPlayerMight(character), false);
		}

		private float GetSpectatedPlayerMight(PlayerCharacter character)
		{
			var gear = character.Gear;
			var currentWeapon = character.CurrentWeapon;

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
	}
}