using System;
using System.Linq;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;

namespace FirstLight.Game.Utils.UCSExtensions
{
	/// <summary>
	/// Helpers for the UCS Friends service.
	/// </summary>
	public static class FriendsServiceExtensions
	{
		/// <summary>
		/// Checks if a user should be considered online.
		/// </summary>
		public static bool IsOnline(this Relationship relationship)
		{
			const int ONLINE_THRESHOLD_MINUTES = 5;

			var presence = relationship.Member.Presence;
			if (presence == null) return false;
			if (presence.Availability != Availability.Online)
			{
				return DateTime.UtcNow - relationship.Member.Presence.LastSeen < TimeSpan.FromMinutes(ONLINE_THRESHOLD_MINUTES);
			}

			return true;
		}

		/// <summary>
		/// Finds a relationship in your friends list by player ID.
		/// </summary>
		public static Relationship GetFriendByID(this IFriendsService friendsService, string playerID)
		{
			return friendsService.Friends.First(r => r.Member.Id == playerID);
		}
	}
}