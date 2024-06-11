using System.Linq;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Helpers for the UCS Friends service.
	/// </summary>
	public static class FriendsServiceExtensions
	{
		/// <summary>
		/// Checks if the availability of a user / presence is online.
		/// </summary>
		public static bool IsOnline(this Presence presence)
		{
			return presence.Availability == Availability.Online;
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