using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using Src.FirstLight.Game.Utils;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
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
		/// Check if the invite is outgoing 
		/// </summary>
		public static bool IsOutgoingInvite(this Relationship r)
		{
			return r.Member.Role == MemberRole.Target;
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
		public static async UniTask<bool> AddFriendByUnityId(this IFriendsService friendsService, string unityId)
		{
			var services = MainInstaller.ResolveServices(); // If a helper need to resolve services then it should be a service itself

			try
			{
				await friendsService.AddFriendAsync(unityId).AsUniTask();
				FLog.Info($"Friend request sent: {unityId}");
			}
			catch (FriendsServiceException e)
			{
				FLog.Warn("Error adding friend.", e);
				services.InGameNotificationService.QueueNotification($"Error adding friend, {e.ParseError()}");
				return false;
			}

			services.InGameNotificationService.QueueNotification("Friend request sent");
			return true;
		}

		/// <summary>
		/// Adds a friend by player name and handles notifications / errors.
		/// </summary>
		public static async UniTask<bool> AddFriendByName(this IFriendsService friendsService, string playerName)
		{
			var services = MainInstaller.ResolveServices(); // If a helper need to resolve services then it should be a service itself

			try
			{
				FLog.Info($"Sending friend request: {playerName}");

				var query = new Query(
					// The first argument to Query is a list of one or more filters, all must evaluate to true for a result to be included
					new List<FieldFilter>
					{
						new ("player_name", playerName, FieldFilter.OpOptions.EQ, true),
					}
				);

				var foundPlayers = await CloudSaveService.Instance.Data.Custom.QueryAsync(query);
				if (foundPlayers.Count == 0)
				{
					services.InGameNotificationService.QueueNotification("Player not found");
					return false;
				}

				var playerId = foundPlayers.First().Id.Replace("read-only-", "");
				await friendsService.AddFriendAsync(playerId).AsUniTask();
				FLog.Info($"Friend request sent: {playerName}");

				services.InGameNotificationService.QueueNotification("Friend request sent");
			}
			catch (FriendsServiceException e)
			{
				FLog.Warn("Error adding friend.", e);
				services.InGameNotificationService.QueueNotification($"Error adding friend, {e.ParseError()}");
				return false;
			}

			return true;
		}

		public static async UniTask<bool> BlockHandled(this IFriendsService friendsService, string playerID)
		{
			var services = MainInstaller.ResolveServices(); // If a helper need to resolve services then it should be a service itself

			try
			{
				FLog.Info($"Blocking player: {playerID}");

				await friendsService.AddBlockAsync(playerID).AsUniTask();

				var tasks = new List<UniTask>();

				var currentInvite = friendsService.Relationships.FirstOrDefault(rl => rl.Type == RelationshipType.FriendRequest && rl.Member.Id == playerID);
				if (currentInvite != null)
				{
					tasks.Add(friendsService.DeleteRelationshipAsync(currentInvite.Id).AsUniTask());
				}

				var friendRelationship = friendsService.GetFriendByID(playerID);
				if (friendRelationship != null)
				{
					tasks.Add(friendsService.DeleteRelationshipAsync(friendRelationship.Id).AsUniTask());
				}

				await UniTask.WhenAll(tasks);

				FLog.Info($"Player blocked: {playerID}");
				services.InGameNotificationService.QueueNotification("Player blocked");
				return true;
			}
			catch (FriendsServiceException e)
			{
				FLog.Warn("Error blocking player", e);

				services.InGameNotificationService.QueueNotification($"Error blocking player, {e.ParseError()}");
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
				services.InGameNotificationService.QueueNotification("Player unblocked");
				return true;
			}
			catch (FriendsServiceException e)
			{
				FLog.Warn("Error unblocking player.", e);
				services.InGameNotificationService.QueueNotification($"Error unblocking player, {e.ParseError()}");
				return false;
			}
		}

		public static async UniTask<bool> RemoveRelationshipHandled(this IFriendsService friendsService, Relationship relationship)
		{
			var services = MainInstaller.ResolveServices(); // If a helper need to resolve services then it should be a service itself

			try
			{
				await friendsService.DeleteRelationshipAsync(relationship.Id).AsUniTask();
				if (relationship.Type == RelationshipType.Block)
				{
					try
					{
						await friendsService.DeleteFriendAsync(relationship.Member.Id).AsUniTask();
					}
					catch (Exception e)
					{
						FLog.Verbose("Could not remove friend, likely was not a friend anymore " + e.Message);
					}
				}

				services.InGameNotificationService.QueueNotification("Player Removed");
				return true;
			}
			catch (FriendsServiceException e)
			{
				services.InGameNotificationService.QueueNotification($"Error removing player, {e.ErrorCode.ToStringSeparatedWords()}");
				return false;
			}
		}

		public static Comparison<Relationship> FriendDefaultSorter()
		{
			var social = MainInstaller.ResolveServices().GameSocialService;
			return (a, b) =>
			{
				if (a.IsOnline() && b.IsOnline())
				{
					if (social.CanInvite(a, out _) && !social.CanInvite(b, out _))
					{
						return -1;
					}

					if (!social.CanInvite(a, out _) && social.CanInvite(b, out _))
					{
						return 1;
					}

					return a.Member.Profile.Name.CompareTo(b.Member.Profile.Name);
				}

				if (a.IsOnline())
				{
					return -1;
				}

				if (b.IsOnline())
				{
					return 1;
				}

				return b.Member.Presence.LastSeen.CompareTo(a.Member.Presence.LastSeen);
			};
		}
	}
}