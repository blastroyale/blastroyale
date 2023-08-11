using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using Photon.Deterministic;
using Quantum;
using Quantum.Core;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Assert = UnityEngine.Assertions.Assert;

namespace FirstLight.Game.Views.UITK
{
	public class MatchStatusView : UIView
	{
		private Label _aliveCountLabel;
		private Label _killsCountLabel;
		private Label _timerLabel;
		private VisualElement _pingElement;
		private VisualElement _counterElement;
		private Label _notificationLabel;

		private IMatchServices _matchServices;
		private IGameServices _gameServices;

		private PlayableDirector _notificationDirector;

		private IVisualElementScheduledItem _timerUpdate;
		private ValueAnimation<float> _pingAnimation;

		private int _aliveCount = -1;
		private int _killsCount = -1;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_gameServices = MainInstaller.Resolve<IGameServices>();

			_aliveCountLabel = element.Q<Label>("AliveCountText").Required();
			_killsCountLabel = element.Q<Label>("KilledCountText").Required();
			_pingElement = element.Q<VisualElement>("PingBG").Required();
			_timerLabel = element.Q<Label>("TimerText").Required();
			_counterElement = element.Q<VisualElement>("Counter");

			_notificationLabel = element.Q<Label>("NotificationText").Required();

			_notificationLabel.SetDisplay(false);

			_pingAnimation = _pingElement.experimental.animation.Scale(0.6f, 1000).KeepAlive();
			_pingAnimation.from = 1f;
		}

		public void SetAreaShrinkingDirector(PlayableDirector director)
		{
			_notificationDirector = director;
		}


		public override void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnNewShrinkingCircle>(this, OnNewShrinkingCircle);
			QuantumEvent.SubscribeManual<EventOnPlayerSpawned>(this, OnPlayerSpawned);
			QuantumEvent.SubscribeManual<EventOnPlayerDead>(this, OnPlayerDead);
			QuantumEvent.SubscribeManual<EventOnAirDropDropped>(this, OnAirDropDropped);
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
		}

		private void OnAirDropDropped(EventOnAirDropDropped callback)
		{
			ShowNotification(ScriptLocalization.UITMatch.airdrop_landing);
		}

		public override void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
			_matchServices.SpectateService.SpectatedPlayer.StopObserving(OnSpectatedPlayerChanged);
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

		private void OnPlayerDead(EventOnPlayerDead callback)
		{
			UpdatePlayerCounts(callback.Game.Frames.Verified);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer current)
		{
			if (!current.Entity.IsValid) return;

			UpdatePlayerCounts(QuantumRunner.Default.Game.Frames.Verified);
		}

		private void UpdatePlayerCounts(FrameBase f)
		{
			var container = f.GetSingleton<GameContainer>();

			// The target is everyone -1 (us) so we need to add 1 to get the full number of players
			var enemiesLeft = container.TargetProgress - container.CurrentProgress + 1;
			if (enemiesLeft != _aliveCount)
			{
				_aliveCount = (int) enemiesLeft;
				_aliveCountLabel.text = enemiesLeft.ToString();
				_aliveCountLabel.AnimatePing();
			}

			var killsCount = container.PlayersData[_matchServices.SpectateService.SpectatedPlayer.Value.Player]
				.PlayersKilledCount;
			if (killsCount != _killsCount)
			{
				_killsCount = (int) killsCount;
				_killsCountLabel.text = killsCount.ToString();
				_killsCountLabel.AnimatePing();
			}
		}

		// This method does the countdowns on Unity side with Unity's timers, so it might not be 100% accurate
		// If that proves to be an issue we may need to rather recalculate the time left using Quantum's Frame.Time.
		private void StartCountdown(int warningStartTime, int shrinkingStartTime, int shrinkingDuration)
		{
			var shrinkingNotified = false;
			var warningNotified = false;

			// FLog.Info("PACO",
			// 	"StartCountdown: delayTime: " + delayTimeMs + " warningTimeMs: " + warningTimeMs + " shrinkingTimeMs: " + shrinkingTimeMs +
			// 	" initialDelayMs: " + initialDelayMs + " delaySeconds: " + delaySeconds + " warningSeconds: " + warningSeconds +
			// 	" shrinkingSeconds: " + shrinkingSeconds + " shrinkingNotified: " + shrinkingNotified + " warningNotified: " + warningNotified + "");

			_timerUpdate?.Pause();
			_timerUpdate = Element.schedule.Execute(() =>
				{
					if (!QuantumRunner.Default.IsDefinedAndRunning()) return;

					var currentTime = QuantumRunner.Default.Game.Frames.Predicted.Time;
					var currentTimeSeconds = FPMath.FloorToInt(currentTime);

					if (currentTimeSeconds < warningStartTime)
					{
						_timerLabel.text = string.Empty;
					}
					else if (currentTimeSeconds < shrinkingStartTime)
					{
						if (!warningNotified)
						{
							ShowNotification(ScriptLocalization.UITMatch.go_to_safe_area);
							warningNotified = true;
						}

						_timerLabel.text = FPMath.RoundToInt(shrinkingStartTime - currentTimeSeconds).ToString();
					}
					else if (currentTimeSeconds < shrinkingStartTime + shrinkingDuration)
					{
						if (!shrinkingNotified)
						{
							ShowNotification(ScriptLocalization.UITMatch.area_shrinking);
							shrinkingNotified = true;
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