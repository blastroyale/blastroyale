using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using Src.FirstLight.Game.Utils;
using Unity.Services.CloudSave;
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
		/// Finds a relationship in your friends list by player ID.
		/// </summary>
		public static Relationship GetRelationShipById(this IFriendsService friendsService, string playerID)
		{
			return friendsService.Relationships.FirstOrDefault(r => r.Member.Id == playerID);
		}

		/// <summary>
		/// Adds a friend by player id and handles notifications / errors.
		/// </summary>
		public static async UniTask<bool> AddFriendHandled(this IFriendsService friendsService, string playerID)
		{
			var services = MainInstaller.ResolveServices(); // If a helper need to resolve services then it should be a service itself

			try
			{
				FLog.Info($"Sending friend request: {playerID}");

				var playfabId = await CloudSaveService.Instance.LoadPlayfabID(playerID);
				if (playfabId == null)
				{
					services.NotificationService.QueueNotification("Player not found");
					return false;
				}

				await friendsService.AddFriendAsync(playerID).AsUniTask();

				FLog.Info($"Friend request sent: {playerID}");

				services.NotificationService.QueueNotification("Friend request sent");
			}
			catch (FriendsServiceException e)
			{
				FLog.Warn("Error adding friend.", e);
				services.NotificationService.QueueNotification($"#Error adding friend, {e.ParseError()}#");
				return false;
			}

			return true;
		}

		public static async UniTask<bool> BlockHandled(this IFriendsService friendsService, string playerID, bool isRequest)
		{
			var services = MainInstaller.ResolveServices(); // If a helper need to resolve services then it should be a service itself

			try
			{
				FLog.Info($"Blocking player: {playerID}");

				if (isRequest)
				{
					await friendsService.DeleteIncomingFriendRequestAsync(playerID);
				}

				var hasOutgoing = friendsService.OutgoingFriendRequests.Any(rl => rl.Id == playerID);
				if (hasOutgoing)
				{
					await friendsService.DeleteOutgoingFriendRequestAsync(playerID);
				}

				await friendsService.AddBlockAsync(playerID).AsUniTask();
				FLog.Info($"Player blocked: {playerID}");

				services.NotificationService.QueueNotification("#Player blocked#");
				return true;
			}
			catch (FriendsServiceException e)
			{
				FLog.Warn("Error blocking player", e);
				services.NotificationService.QueueNotification($"#Error blocking player, {e.ErrorCode.ToStringSeparatedWords()}#");
				return false;
			}
		}

		public static async UniTask<bool> UnblockHandled(this IFriendsService friendsService, Relationship r)
		{
			var services = MainInstaller.ResolveServices(); // If a helper need to resolve services then it should be a service itself

			try
			{
				FLog.Info($"Unblocking player: {r.Member.Id}");
				await friendsService.DeleteBlockAsync(r.Member.Id).AsUniTask();
				FLog.Info($"Player unblocked: {r.Member.Id}");
				services.NotificationService.QueueNotification("#Player unblocked#");
				return true;
			}
			catch (FriendsServiceException e)
			{
				FLog.Warn("Error unblocking player.", e);
				services.NotificationService.QueueNotification($"#Error unblocking player, {e.ErrorCode.ToStringSeparatedWords()}#");
				return false;
			}
		}
	}
}