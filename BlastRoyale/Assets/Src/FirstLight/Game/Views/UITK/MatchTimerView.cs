using FirstLight.Game.Ids;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using I2.Loc;
using Photon.Deterministic;
using Quantum;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.Views.UITK
{
	public class MatchTimerView : UIView
	{
		private Label _timerLabel;
		private VisualElement _pingElement;
		private VisualElement _counterElement;
		private Label _notificationLabel;

		private PlayableDirector _notificationDirector;

		private IVisualElementScheduledItem _timerUpdate;
		private ValueAnimation<float> _pingAnimation;
		private IGameServices _services;
		private int _lastKnownStep = 0;

		protected override void Attached()
		{
			_pingElement = Element.Q<VisualElement>("PingBG").Required();
			_timerLabel = Element.Q<Label>("TimerText").Required();
			_counterElement = Element.Q<VisualElement>("Counter");

			_notificationLabel = Element.Q<Label>("NotificationText").Required();

			_notificationLabel.SetDisplay(false);

			_pingAnimation = _pingElement.experimental.animation.Scale(0.6f, 1000).KeepAlive();
			_pingAnimation.from = 1f;

			_services = MainInstaller.Resolve<IGameServices>();
		}


		public override void OnScreenOpen(bool reload)
		{
			QuantumEvent.SubscribeManual<EventOnNewShrinkingCircle>(this, OnNewShrinkingCircle);
			QuantumEvent.SubscribeManual<EventOnAirDropDropped>(this, OnAirDropDropped);
			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
		}

		public override void OnScreenClose()
		{
			QuantumEvent.UnsubscribeListener(this);
			_services.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OnMatchStarted(MatchStartedMessage msg)
		{
			var game = msg.Game;
			var f = game.Frames.Verified;
			UpdateCircleBasedOnFrame(f);
		}

		public void SetAreaShrinkingDirector(PlayableDirector director)
		{
			_notificationDirector = director;
		}


		private void OnAirDropDropped(EventOnAirDropDropped callback)
		{
			ShowNotification(ScriptLocalization.UITMatch.airdrop_landing);
		}


		private void OnNewShrinkingCircle(EventOnNewShrinkingCircle callback)
		{
			UpdateCircleBasedOnFrame(callback.Game.Frames.Verified);
		}


		private void UpdateCircleBasedOnFrame(Frame f)
		{
			if (!f.Context.GameModeConfig.ShowUITimer)
			{
				_counterElement.SetVisibility(false);
				return;
			}

			_counterElement.SetVisibility(true);
			if (!f.TryGetSingleton<ShrinkingCircle>(out var circle)) return;
			if (_lastKnownStep >= circle.Step) return;

			_lastKnownStep = circle.Step;
			StartCountdown(circle);
		}


		// This method does the countdowns on Unity side with Unity's timers, so it might not be 100% accurate
		// If that proves to be an issue we may need to rather recalculate the time left using Quantum's Frame.Time.
		private void StartCountdown(ShrinkingCircle circle)
		{
			var warningStartTime = circle.ShrinkingStartTime - circle.ShrinkingWarningTime;
			var shrinkingStartTime = circle.ShrinkingStartTime;
			var shrinkingDuration = circle.ShrinkingDurationTime;

			var shrinkingNotified = false;
			var warningNotified = false;
			var delayPhaseStarted = false;

			_timerUpdate?.Pause();
			_timerUpdate = Element.schedule.Execute(() =>
				{
					if (!QuantumRunner.Default.IsDefinedAndRunning(false)) return;

					var currentTime = QuantumRunner.Default.Game.Frames.Predicted.Time;
					var currentTimeSeconds = FPMath.FloorToInt(currentTime);

					if (currentTimeSeconds < warningStartTime)
					{
						if (!delayPhaseStarted)
						{
							_timerLabel.text = string.Empty;
							_counterElement.SetVisibility(false);
							delayPhaseStarted = true;
						}
					}
					else if (currentTimeSeconds < shrinkingStartTime)
					{
						if (!warningNotified)
						{
							_counterElement.SetVisibility(true);
							ShowNotification(ScriptLocalization.UITMatch.go_to_safe_area);
							warningNotified = true;

							_services.AudioFxService.PlayClip2D(AudioId.GoToSafeZone, GameConstants.Audio.MIXER_GROUP_SFX_2D_ID);
						}

						_timerLabel.text = FPMath.RoundToInt(shrinkingStartTime - currentTimeSeconds).ToString();
					}
					else if (currentTimeSeconds < shrinkingStartTime + shrinkingDuration)
					{
						if (!shrinkingNotified)
						{
							_counterElement.SetVisibility(true);
							ShowNotification(ScriptLocalization.UITMatch.area_shrinking);
							shrinkingNotified = true;

							_services.AudioFxService.PlayClip2D(AudioId.CircleIsClosing, GameConstants.Audio.MIXER_GROUP_SFX_2D_ID);
						}

						_timerLabel.text = FPMath.RoundToInt((shrinkingStartTime + shrinkingDuration) - currentTimeSeconds).ToString();
						_pingAnimation.Start();
					}
					else
					{
						_timerLabel.text = string.Empty;
					}
				})
				.StartingIn((FPMath.Fraction(QuantumRunner.Default.Game.Frames.Predicted.Time) * FP._1000).AsLong +
					100) // 100ms offset so we don't skip numbers because we round down.
				.Every(1000)
				.Until(() => !QuantumRunner.Default.IsDefinedAndRunning(false) ||
					QuantumRunner.Default.Game.Frames.Predicted.Time > shrinkingStartTime + shrinkingDuration);
		}

		private void ShowNotification(string message)
		{
			_notificationLabel.text = message;
			_notificationDirector.Play();
		}
	}
}