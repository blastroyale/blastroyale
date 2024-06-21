using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
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
			return friendsService.Friends.FirstOrDefault(r => r.Member.Id == playerID);
		}

		/// <summary>
		/// Adds a friend by player id and handles notifications / errors.
		/// </summary>
		public static async UniTask<bool> AddFriendHandled(this IFriendsService friendsService, string playerID)
		{
			var services = MainInstaller.ResolveServices();

			try
			{
				FLog.Info($"Sending friend request: {playerID}");
				await friendsService.AddFriendAsync(playerID).AsUniTask();
				FLog.Info($"Friend request sent: {playerID}");

				services.NotificationService.QueueNotification("Friend request sent");
			}
			catch (FriendsServiceException e)
			{
				FLog.Error("Error adding friend.", e);
				services.NotificationService.QueueNotification($"#Error adding friend ({(int) e.ErrorCode})#");
				return false;
			}

			return true;
		}
	}
}