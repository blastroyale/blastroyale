using FirstLight.Game.Ids;
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
	public class MatchTimerView : UIView2
	{
		private Label _timerLabel;
		private VisualElement _pingElement;
		private VisualElement _counterElement;
		private Label _notificationLabel;

		private PlayableDirector _notificationDirector;

		private IVisualElementScheduledItem _timerUpdate;
		private ValueAnimation<float> _pingAnimation;
		private IGameServices _services;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);

			_pingElement = element.Q<VisualElement>("PingBG").Required();
			_timerLabel = element.Q<Label>("TimerText").Required();
			_counterElement = element.Q<VisualElement>("Counter");

			_notificationLabel = element.Q<Label>("NotificationText").Required();

			_notificationLabel.SetDisplay(false);

			_pingAnimation = _pingElement.experimental.animation.Scale(0.6f, 1000).KeepAlive();
			_pingAnimation.from = 1f;

			_services = MainInstaller.Resolve<IGameServices>();
		}

		public void SetAreaShrinkingDirector(PlayableDirector director)
		{
			_notificationDirector = director;
		}


		public override void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnNewShrinkingCircle>(this, OnNewShrinkingCircle);
			QuantumEvent.SubscribeManual<EventOnPlayerSpawned>(this, OnPlayerSpawned);
			QuantumEvent.SubscribeManual<EventOnAirDropDropped>(this, OnAirDropDropped);
		}

		private void OnAirDropDropped(EventOnAirDropDropped callback)
		{
			ShowNotification(ScriptLocalization.UITMatch.airdrop_landing);
		}

		public override void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnNewShrinkingCircle(EventOnNewShrinkingCircle callback)
		{
			if (!callback.Game.Frames.Verified.Context.GameModeConfig.ShowUITimer)
			{
				return;
			}

			var warningStart = callback.ShrinkingCircle.ShrinkingStartTime - callback.ShrinkingCircle.ShrinkingWarningTime;
			var shrinkingStart = callback.ShrinkingCircle.ShrinkingStartTime;
			var shrinkingDuration = callback.ShrinkingCircle.ShrinkingDurationTime;

			StartCountdown(warningStart, shrinkingStart, shrinkingDuration);
		}

		private void OnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (!callback.Game.Frames.Verified.Context.GameModeConfig.ShowUITimer)
			{
				_counterElement.SetVisibility(false);
				return;
			}

			_counterElement.SetVisibility(true);
			if (_timerUpdate == null && callback.Game.Frames.Predicted.TryGetSingleton<ShrinkingCircle>(out var circle))
			{
				var warningStart = circle.ShrinkingStartTime - circle.ShrinkingWarningTime;
				var shrinkingStart = circle.ShrinkingStartTime;
				var shrinkingDuration = circle.ShrinkingDurationTime;

				StartCountdown(warningStart, shrinkingStart, shrinkingDuration);
			}
		}

		// This method does the countdowns on Unity side with Unity's timers, so it might not be 100% accurate
		// If that proves to be an issue we may need to rather recalculate the time left using Quantum's Frame.Time.
		private void StartCountdown(int warningStartTime, int shrinkingStartTime, int shrinkingDuration)
		{
			var shrinkingNotified = false;
			var warningNotified = false;
			var delayPhaseStarted = false;

			_timerUpdate?.Pause();
			_timerUpdate = Element.schedule.Execute(() =>
				{
					if (!QuantumRunner.Default.IsDefinedAndRunning()) return;

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
				.Until(() => !QuantumRunner.Default.IsDefinedAndRunning() ||
					QuantumRunner.Default.Game.Frames.Predicted.Time > shrinkingStartTime + shrinkingDuration);
		}

		private void ShowNotification(string message)
		{
			_notificationLabel.text = message;
			_notificationDirector.Play();
		}
	}
}