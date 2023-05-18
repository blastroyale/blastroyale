using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using Quantum.Core;
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

		private IMatchServices _matchServices;

		private IVisualElementScheduledItem _timerUpdate;
		private ValueAnimation<float> _pingAnimation;

		private int _aliveCount = -1;
		private int _killsCount = -1;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_aliveCountLabel = element.Q<Label>("AliveCountText");
			_killsCountLabel = element.Q<Label>("KilledCountText");
			_pingElement = element.Q<VisualElement>("PingBG");
			_timerLabel = element.Q<Label>("TimerText");

			_pingAnimation = _pingElement.experimental.animation.Scale(0.6f, 1000).KeepAlive();
			_pingAnimation.from = 1f;
		}

		public override void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnNewShrinkingCircle>(this, OnNewShrinkingCircle);
			QuantumEvent.SubscribeManual<EventOnPlayerSpawned>(this, OnPlayerSpawned);
			QuantumEvent.SubscribeManual<EventOnPlayerDead>(this, OnPlayerDead);
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
		}

		public override void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
			_matchServices.SpectateService.SpectatedPlayer.StopObserving(OnSpectatedPlayerChanged);
		}

		private void OnNewShrinkingCircle(EventOnNewShrinkingCircle callback)
		{
			var countdown = ((callback.ShrinkingCircle.ShrinkingStartTime - callback.Game.Frames.Predicted.Time) * 1000)
				.AsLong;

			StartCountdown(countdown, (callback.ShrinkingCircle.ShrinkingDurationTime * 1000).AsLong);
		}

		private void OnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (_timerUpdate == null && callback.Game.Frames.Predicted.TryGetSingleton<ShrinkingCircle>(out var circle))
			{
				var countdown = ((circle.ShrinkingStartTime - callback.Game.Frames.Predicted.Time) * 1000).AsLong;
				StartCountdown(countdown, (circle.ShrinkingDurationTime * 1000).AsLong);
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

		public void StartCountdown(long warningTimeMs, long shrinkingTimeMs)
		{
			Assert.IsTrue(shrinkingTimeMs % 1000 == 0, "Shrinking time must be rounded to seconds!");

			var initialDelayMs = warningTimeMs % 1000;

			var warningSeconds = (int) (warningTimeMs / 1000);
			var shrinkingSeconds = (int) (shrinkingTimeMs / 1000);

			_timerUpdate?.Pause();
			_timerUpdate = Element.schedule.Execute(() =>
				{
					if (warningSeconds > 0)
					{
						_timerLabel.text = warningSeconds.ToString();
						warningSeconds--;
					}
					else if (shrinkingSeconds > 0)
					{
						_timerLabel.text = shrinkingSeconds.ToString();
						shrinkingSeconds--;
						_pingAnimation.Start();
					}
					else
					{
						_timerLabel.text = string.Empty;
					}
				}).StartingIn(initialDelayMs)
				.Every(1000)
				.Until(() =>
					warningSeconds == 0 &&
					shrinkingSeconds == -1); // -1 because we want to show empty string after the countdown
		}
	}
}