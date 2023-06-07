using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Shows players that have been killed and by whom.
	/// </summary>
	public class KillFeedView : UIView
	{
		private const long SHOWN_DURATION = 5000;
		private const long RELEASED_AFTER = 6000;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			// Clean feed items added during development
			element.Clear();
		}

		public override void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
		}

		public override void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			// Do nothing in case of offline Tutorial match where there are bots on the map which are not in PlayersMatchData
			if (callback.PlayersMatchData.Count <= 1)
			{
				return;
			}
			
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

			Element.Add(deathNotification);
		}
	}
}