using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	public class PlayerCountsView : UIView
	{
		private Label _aliveCountLabel;
		private VisualElement _aliveCountPing;
		private Label _killsCountLabel;
		private VisualElement _killsCountPing;

		private IMatchServices _matchServices;

		private int _aliveCount = -1;
		private int _killsCount = -1;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_aliveCountLabel = element.Q<Label>("AliveCountText").Required();
			_aliveCountPing = element.Q<VisualElement>("AliveCountPing").Required();
			_killsCountLabel = element.Q<Label>("KilledCountText").Required();
			_killsCountPing = element.Q<VisualElement>("KilledCountPing").Required();
		}

		public override void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnPlayerDead>(this, OnPlayerDead);
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
		}

		public override void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
			_matchServices.SpectateService.SpectatedPlayer.StopObserving(OnSpectatedPlayerChanged);
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

		private void UpdatePlayerCounts(Frame f)
		{
			var container = f.GetSingleton<GameContainer>();

			// We count all alive players in a match and display this number
			var playersAlive = 0;
			for (int i = 0; i < container.PlayersData.Length; i++)
			{
				var data = container.PlayersData[i];

				if (data.IsValid && data.Entity.IsAlive(f))
				{
					playersAlive++;
				}
			}

			_aliveCount = playersAlive;
			_aliveCountLabel.text = playersAlive.ToString();
			_aliveCountLabel.AnimatePing();
			_aliveCountPing.AnimatePingOpacity();

			var killsCount = container.PlayersData[_matchServices.SpectateService.SpectatedPlayer.Value.Player]
				.PlayersKilledCount;
			if (killsCount != _killsCount)
			{
				_killsCount = (int) killsCount;
				_killsCountLabel.text = killsCount.ToString();
				_killsCountLabel.AnimatePing();
				_killsCountPing.AnimatePingOpacity();
			}
		}
	}
}