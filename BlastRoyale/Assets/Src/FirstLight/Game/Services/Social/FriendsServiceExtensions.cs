using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using Src.FirstLight.Game.Utils;
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
			var presence = relationship.Member.Presence;
			if (presence == null) return false;

			return presence.Availability == Availability.Online;
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
		public static async UniTask<bool> AddFriendHandled(this IFriendsService friendsService, string playerIDOrName)
		{
			var services = MainInstaller.ResolveServices();

			try
			{
				FLog.Info($"Sending friend request: {playerIDOrName}");
				if (playerIDOrName.Contains('#'))
				{
					await friendsService.AddFriendByNameAsync(playerIDOrName).AsUniTask();
				}
				else
				{
					await friendsService.AddFriendAsync(playerIDOrName).AsUniTask();
				}

				FLog.Info($"Friend request sent: {playerIDOrName}");

				services.NotificationService.QueueNotification("Friend request sent");
			}
			catch (FriendsServiceException e)
			{
				FLog.Error("Error adding friend.", e);
				services.NotificationService.QueueNotification($"#Error adding friend, {e.ErrorCode.ToString().CamelCaseToSeparatedWords()}#");
				return false;
			}
			return true;
		}
	}
}