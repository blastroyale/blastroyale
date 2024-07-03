using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Collection;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Shows players that have been killed and by whom.
	/// </summary>
	public class KillFeedView : UIView
	{
		private const long SHOWN_DURATION = 5000;
		private const long RELEASE_DELAY = 1000;
		private const long RELEASED_AFTER = SHOWN_DURATION + RELEASE_DELAY;
		private const long MAX_VISIBLE = 3;

		private IMatchServices _matchServices;
		private IGameServices _gameServices;
		private ICollectionService _collectionService;

		private readonly List<DeathNotificationElement> _visibleNotifications = new ();

		protected override void Attached()
		{
			// Clean feed items added during development
			Element.Clear();

			_matchServices = MainInstaller.ResolveMatchServices();
			_gameServices = MainInstaller.Resolve<IGameServices>();
			_collectionService = _gameServices.CollectionService;
		}

		public override void OnScreenOpen(bool reload)
		{
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
		}

		public override void OnScreenClose()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private async void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			// Do nothing in case of offline Tutorial match where there are bots on the map which are not in PlayersMatchData
			if (callback.PlayersMatchData.Count <= 1)
			{
				return;
			}

			var killerData = callback.PlayersMatchData[callback.PlayerKiller];
			var victimData = callback.PlayersMatchData[callback.PlayerDead];

			var killerNameColor =
				_gameServices.LeaderboardService.GetRankColor(_gameServices.LeaderboardService.Ranked, (int) killerData.LeaderboardRank);
			var victimNameColor =
				_gameServices.LeaderboardService.GetRankColor(_gameServices.LeaderboardService.Ranked, (int) victimData.LeaderboardRank);

			var killerFriendly = killerData.TeamId == _matchServices.SpectateService.SpectatedPlayer.Value.Team;
			var victimFriendly = victimData.TeamId == _matchServices.SpectateService.SpectatedPlayer.Value.Team;
			
			var killerCosmetics = callback.Game.Frames.Verified.ResolveList(killerData.Data.Cosmetics);
			var victimCosmetics = callback.Game.Frames.Verified.ResolveList(victimData.Data.Cosmetics);
			
			var killerCharacterPfpSprite = await _collectionService.LoadCollectionItemSprite(_collectionService.GetCosmeticForGroup(killerCosmetics, GameIdGroup.PlayerSkin));
			var victimCharacterPfpSprite = await _collectionService.LoadCollectionItemSprite(_collectionService.GetCosmeticForGroup(victimCosmetics, GameIdGroup.PlayerSkin));
			
			SpawnDeathNotification(killerData.GetPlayerName(), killerFriendly, killerCharacterPfpSprite, victimData.GetPlayerName(), victimFriendly,
				victimCharacterPfpSprite, killerData.Data.Player == victimData.Data.Player, killerNameColor, victimNameColor);
		}

		private void SpawnDeathNotification(string killerName, bool killerFriendly, Sprite killerCharacterPfpSprite, string victimName,
											bool victimFriendly, Sprite victimCharacterPfpSprite, bool suicide, StyleColor killerColor, StyleColor victimColor)
		{
			// TODO: Add a pool for this
			var deathNotification = new DeathNotificationElement(killerName, killerFriendly, killerCharacterPfpSprite, victimName, victimFriendly,
				victimCharacterPfpSprite, suicide, killerColor, victimColor);
			deathNotification.Hide(false);

			deathNotification.schedule.Execute(() =>
				{
					_visibleNotifications.Add(deathNotification);
					deathNotification.Show();
				})
				.StartingIn(10);
			deathNotification.schedule.Execute(() =>
				{
					if (deathNotification.JobDone) return;
					_visibleNotifications.Remove(deathNotification);
					deathNotification.Hide(true);
				})
				.StartingIn(SHOWN_DURATION);
			deathNotification.schedule.Execute(() =>
				{
					if (deathNotification.parent == null) return;
					deathNotification.RemoveFromHierarchy();
				})
				.StartingIn(RELEASED_AFTER);

			if (_visibleNotifications.Count >= MAX_VISIBLE)
			{
				_visibleNotifications[0].Hide(true);
				_visibleNotifications.RemoveAt(0);
				deathNotification.schedule.Execute(() => deathNotification.RemoveFromHierarchy())
					.StartingIn(RELEASE_DELAY);
			}

			Element.Add(deathNotification);
		}
	}
}