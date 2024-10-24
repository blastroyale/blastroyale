using System;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.UIService;
using Unity.Services.Lobbies;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.MainMenuViews
{
	public class GameModeButtonView : UIView
	{
		public const string USS_EVENT = "game-mode-button--event";

		private ImageButton _gameModeButton;
		private Label _gameModeLabel;
		private VisualElement _gameModeIcon;
		private IGameServices _services;
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
			if (!reload)
			{
				_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyUpdated += OnLobbyChanged;
			}

			_services.GameModeService.SelectedGameMode.InvokeObserve(OnSelectedGameModeChanged);
		}

		public override void OnScreenClose()
		{
			_services.MessageBrokerService.UnsubscribeAll(this);
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyUpdated -= OnLobbyChanged;
			_services.GameModeService.SelectedGameMode.StopObserving(OnSelectedGameModeChanged);
		}

		private void OnLobbyChanged(ILobbyChanges m)
		{
			UpdateGameModeButton();
		}

		private void OnSelectedGameModeChanged(GameModeInfo _, GameModeInfo current)
		{
			UpdateGameModeButton();
		}

		private void UpdateGameModeButton()
		{
			FLog.Verbose("Updating game mode button");
			_updateSchedule?.Pause();
			var current = _services.GameModeService.SelectedGameMode.Value.Entry;
			var currentTeamSetting = _services.GameModeService.GetTeamSizeFor(current);
			var isMemberNotLeader = _services.FLLobbyService.CurrentPartyLobby != null && !_services.FLLobbyService.CurrentPartyLobby.IsLocalPlayerHost();
			_gameModeLabel.text = current.Title.GetText();
			_gameModeButton.SetEnabled(!isMemberNotLeader);
			_gameModeIcon.RemoveSpriteClasses();

			_gameModeIcon.AddToClassList(currentTeamSetting.IconSpriteClass);
			_updateSchedule = _eventCountDown.schedule.Execute(UpdateEvent)
				.StartingIn(0)
				.Every(5000);
		}

		private bool showSeconds = false;
		private Action _pingAnimationCancel;
		private VisualElement _newEventGlow;

		private void UpdateEvent()
		{
			if (_services.FLLobbyService.IsInPartyLobby())
			{
				CancelEventEffects();
				return;
			}

			var nextEvent = _services.GameModeService.Slots.FirstOrDefault(a => a.Entry is EventGameModeEntry);
			if (nextEvent.Entry != null)
			{
				var now = DateTime.UtcNow;
				if (!nextEvent.Duration.Contains(now))
				{
					_nextEventContainer.SetDisplay(true);
					var diff = nextEvent.Duration.GetStartsAtDateTime() - now;
					if (diff.TotalMinutes < 1 && !showSeconds)
					{
						showSeconds = true;
						_updateSchedule.Every(1000);
					}

					_eventCountDown.text = diff.Display(showSeconds: showSeconds, showHours: !showSeconds, showDays: !showSeconds);
					return;
				}

				if (!_services.GameModeService.HasSeenEvent(nextEvent))
				{
					if (_pingAnimationCancel == null)
					{
						_pingAnimationCancel = Element.AnimatePingRepeating(amount: 1.05f, duration: 200, delay: 1500);
						_newEventShine.AnimatePingOpacity(fromAmount: 0.5f, toAmount: 1f, duration: 1000, repeat: true);
						_newEventGlow.AnimatePingOpacity(fromAmount: 0.5f, toAmount: 1f, duration: 1000, repeat: true);
						_newEventShine.AddRotatingEffect(40f, 10);
					}

					Element.AddToClassList(USS_EVENT);
				}
			}

			_nextEventContainer.SetDisplay(false);
		}

		private void CancelEventEffects()
		{
			if (_pingAnimationCancel != null)
			{
				_pingAnimationCancel();
				_pingAnimationCancel = null;
			}

			Element?.RemoveFromClassList(USS_EVENT);
		}
	}
}