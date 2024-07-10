using System;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Party;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.Views.MainMenuViews
{
	public class GameModeButtonView : UIView
	{
		private ImageButton _gameModeButton;
		private Label _gameModeLabel;
		private VisualElement _gameModeIcon;
		private IGameServices _services;
		private IPartyService _partyService;
		private Label _eventCountDown;
		private VisualElement _nextEventContainer;
		private VisualElement _newEventShine;
		private IVisualElementScheduledItem _updateSchedule;

		public GameModeButtonView()
		{
		}

		protected override void Attached()
		{
			_services = MainInstaller.ResolveServices();
			_partyService = _services.PartyService;
			_gameModeLabel = Element.Q<Label>("GameModeLabel").Required();
			_gameModeIcon = Element.Q<VisualElement>("GameModeIcon").Required();
			_gameModeButton = Element.Q<ImageButton>("GameModeButton").Required();
			_nextEventContainer = Element.Q<VisualElement>("NextEventContainer").Required();
			_newEventShine = Element.Q<VisualElement>("NewEventShine").Required();
			_newEventGlow = Element.Q<VisualElement>("NewEventGlow").Required();
			_eventCountDown = Element.Q<Label>("NextEventCountdown").Required();
		}

		public override void OnScreenOpen(bool reload)
		{
			_partyService.HasParty.InvokeObserve(OnHasPartyChanged);
			_partyService.PartyReady.InvokeObserve(OnPartyReadyChanged);
			_partyService.Members.Observe(OnMembersChanged);
			_partyService.OperationInProgress.InvokeObserve(OnPartyLoadingProgress);
			_services.GameModeService.SelectedGameMode.InvokeObserve(OnSelectedGameModeChanged);
		}

		public override void OnScreenClose()
		{
			_partyService.HasParty.StopObserving(OnHasPartyChanged);
			_partyService.PartyReady.StopObserving(OnPartyReadyChanged);
			_partyService.Members.StopObserving(OnMembersChanged);
			_partyService.OperationInProgress.StopObserving(OnPartyLoadingProgress);
			_services.GameModeService.SelectedGameMode.StopObserving(OnSelectedGameModeChanged);
		}

		private void OnPartyLoadingProgress(bool arg1, bool arg2)
		{
			UpdateGameModeButton();
		}

		private void OnMembersChanged(int arg1, PartyMember arg2, PartyMember arg3, ObservableUpdateType arg4)
		{
			UpdateGameModeButton();
		}

		private void OnPartyReadyChanged(bool arg1, bool arg2)
		{
			UpdateGameModeButton();
		}

		private void OnHasPartyChanged(bool arg1, bool arg2)
		{
			UpdateGameModeButton();
		}

		private void OnSelectedGameModeChanged(GameModeInfo _, GameModeInfo current)
		{
			UpdateGameModeButton();
		}

		private void UpdateGameModeButton()
		{
			_updateSchedule?.Pause();
			var current = _services.GameModeService.SelectedGameMode.Value.Entry;

			var localMember = _services.PartyService.GetLocalMember();
			var isMemberNotLeader = _services.PartyService.HasParty.Value && localMember is {Leader: false};
			_gameModeLabel.text = current.Visual.TitleTranslationKey.GetText();
			_gameModeButton.SetEnabled(!isMemberNotLeader && !_services.PartyService.OperationInProgress.Value);
			_gameModeIcon.RemoveSpriteClasses();
			_gameModeIcon.AddToClassList(current.Visual.IconSpriteClass);
			_updateSchedule = _eventCountDown.schedule.Execute(UpdateEvent)
				.StartingIn(0)
				.Every(5000);
		}

		private bool showSeconds = false;
		private IValueAnimation _pingAnimation;
		private VisualElement _newEventGlow;

		private void UpdateEvent()
		{
			if (_services.GameModeService.TryGetNextEvent(out var info))
			{
				var now = DateTime.UtcNow;
				if (!info.Duration.Contains(now))
				{
					_nextEventContainer.SetDisplay(true);
					var diff = info.Duration.GetStartsAtDateTime() - now;
					if (diff.TotalMinutes < 1 && !showSeconds)
					{
						showSeconds = true;
						_updateSchedule.Every(1000);
					}

					_eventCountDown.text = diff.Display(showSeconds: showSeconds, showHours: !showSeconds, showDays: !showSeconds);
					return;
				}

				if (_services.LocalPrefsService.LastSeenEvent != info.GetKey())
				{
					if (_pingAnimation == null)
					{
						_pingAnimation = Element.AnimatePing(amount: 1.05f, duration: 200, repeat: true, delay: 1500);
						_newEventShine.AnimatePingOpacity(fromAmount: 0.5f, toAmount: 1f, duration: 1000, repeat: true);
						_newEventGlow.AnimatePingOpacity(fromAmount: 0.5f, toAmount: 1f, duration: 1000, repeat: true);
						_newEventShine.AddRotatingEffect(40f, 10);
					}

					Element.AddToClassList("game-mode-button--event");
				}
			}

			_nextEventContainer.SetDisplay(false);
		}
	}
}