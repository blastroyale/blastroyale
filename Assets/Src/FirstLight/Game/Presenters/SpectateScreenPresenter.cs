using System;
using System.Collections.Generic;
using Cinemachine;
using FirstLight.FLogger;
using FirstLight.Game.Infos;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
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

		// ReSharper disable NotAccessedField.Local
		private StatusBarsView _statusBarsView;
		// ReSharper reset NotAccessedField.Local

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
			root.Q("StatusBars").Required().AttachView(this, out _statusBarsView);
			_statusBarsView.ForceOverheadUI();
			_statusBarsView.InitAll();

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
			var gamemodeID = _services.RoomService.CurrentRoom.Properties.GameModeId.Value;
			_header.SetSubtitle(gamemodeID.ToUpper());
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

			if (!current.Player.IsValid)
			{
				FLog.Warn($"Invalid player entity {current.Entity} being spectated");
				return;
			}
			
			if (!f.TryGet<PlayerCharacter>(current.Entity, out var playerCharacter))
			{
				return;
			}

			var data = new QuantumPlayerMatchData(f, playersData[current.Player]);
			var nameColor = _services.LeaderboardService.GetRankColor(_services.LeaderboardService.Ranked, (int) data.LeaderboardRank);

			_followCamera.Follow = current.Transform;
			_followCamera.LookAt = current.Transform;
			_followCamera.SnapCamera();

			_playerName.text = data.GetPlayerName();
			_playerName.style.color = nameColor;
			_defeatedYou.SetVisibility(current.Player == _matchServices.MatchEndDataService.LocalPlayerKiller);
			UpdateCurrentMight(playerCharacter);
		}

		private void UpdateCurrentMight(PlayerCharacter character)
		{
			_playerMight.SetMight(GetSpectatedPlayerMight(character), false);
		}

		private float GetSpectatedPlayerMight(PlayerCharacter character)
		{
			var currentWeapon = character.CurrentWeapon;

			var currentEquipment = new List<Equipment>(6);
			if (currentWeapon.IsValid())
			{
				currentEquipment.Add(currentWeapon);
			}

			foreach (var item in  character.GetLoadoutGear(QuantumRunner.Default.VerifiedFrame()))
			{
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