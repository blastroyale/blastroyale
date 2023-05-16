using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	public class KillFeedView : IUIView
	{
		private const long SHOWN_DURATION = 5000;
		private const long RELEASED_AFTER = 6000;

		private VisualElement _feed;

		public void Attached(VisualElement feed)
		{
			_feed = feed;

			// Clean feed items added during development
			feed.Clear();
		}

		public void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
		}

		public void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			var killerData = callback.PlayersMatchData[callback.PlayerKiller];
			var victimData = callback.PlayersMatchData[callback.PlayerDead];

			SpawnDeathNotification(killerData.GetPlayerName(), victimData.GetPlayerName());
		}

		public void SpawnDeathNotification(string killerName, string victimName)
		{
			// TODO: Add a pool for this
			var deathNotification = new DeathNotificationElement();
			deathNotification.Hide(false);
			deathNotification.SetData(killerName, victimName);

			deathNotification.schedule.Execute(() => deathNotification.Show())
				.StartingIn(10);
			deathNotification.schedule.Execute(() => deathNotification.Hide(true))
				.StartingIn(SHOWN_DURATION);
			deathNotification.schedule.Execute(() => deathNotification.RemoveFromHierarchy())
				.StartingIn(RELEASED_AFTER);

			_feed.Add(deathNotification);
		}
	}
}