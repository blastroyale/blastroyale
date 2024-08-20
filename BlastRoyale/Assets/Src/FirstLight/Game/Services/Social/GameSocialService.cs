using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Best.HTTP.Shared.PlatformSupport.IL2CPP;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes.Helpers;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using I2.Loc;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Quantum;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using UnityEngine.UIElements;
using Unity.Services.Lobbies.Models;

namespace FirstLight.Game.Services
{
	public enum GameActivities
	{
		In_team,
		In_main_menu,
		In_game_lobby,
		In_matchmaking,
		In_match,
		Spectating
	}

	public static class GameActivitiesExtensions
	{
		public static bool CanReceiveInvite(this GameActivities activities)
		{
			return activities == GameActivities.In_main_menu;
		}
	}

	[DataContract, Preserve]
	public class FriendActivity
	{
		[Preserve, DataMember(Name = "status", IsRequired = true, EmitDefaultValue = true)]
		public int CurrentActivity { get; set; }

		[Preserve, DataMember(Name = "avatar", IsRequired = true, EmitDefaultValue = true)]
		public string AvatarUrl { get; set; }

		[Preserve, DataMember(Name = "region", IsRequired = false, EmitDefaultValue = true), CanBeNull]
		public string Region { get; set; }

		[Preserve, DataMember(Name = "trophies", IsRequired = false, EmitDefaultValue = true)]
		public int Trophies { get; set; }

		public string Status => CurrentActivityEnum.ToString().Replace(@"_", " ");
		public GameActivities CurrentActivityEnum => ((GameActivities) CurrentActivity);
	}

	public class PlayerContextSettings
	{
		public bool ShowTeamOptions = false;
		public bool ShowRemoveFriend = false;
		public bool ShowBlock = false;
		public Action OnRelationShipChange;
		public IEnumerable<PlayerContextButton> ExtraButtons;
		public TooltipPosition Position = TooltipPosition.Auto;
	}

	public interface IGameSocialService
	{
		bool CanInvite(Relationship friend, out string reason);
		bool CanAddFriend(Player friend);
		void SetCurrentActivity(GameActivities activity);
		public GameActivities GetCurrentPlayerActivity();
		UniTask FakeInviteBot(string botName);
		bool IsBotInvited(string botName);
		public void OpenPlayerOptions(VisualElement element, VisualElement root, string unityId, string playerName, PlayerContextSettings settings = null);
	}

	public class GameSocialService : IGameSocialService
	{
		private BufferedQueue _stateUpdates = new (TimeSpan.FromSeconds(3), true);
		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private HashSet<string> _fakeBotRequests = new ();
		private FriendActivity _playerActivity = new ();

		public GameSocialService(IGameServices services, IGameDataProvider dataProvider)
		{
			_services = services;
			_dataProvider = dataProvider;
			services.FLLobbyService.CurrentPartyCallbacks.LobbyDeleted += UpdateCurrentPlayerActivity;
			services.FLLobbyService.CurrentPartyCallbacks.KickedFromLobby += UpdateCurrentPlayerActivity;
			services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyJoined += _ => OnJoinedParty();

			services.FLLobbyService.CurrentMatchCallbacks.LocalLobbyJoined += _ => UpdateCurrentPlayerActivity();
			services.FLLobbyService.CurrentMatchCallbacks.KickedFromLobby += UpdateCurrentPlayerActivity;
			services.FLLobbyService.CurrentMatchCallbacks.LobbyDeleted += UpdateCurrentPlayerActivity;
			services.FLLobbyService.CurrentMatchCallbacks.PlayerLeft += (_) => UpdateCurrentPlayerActivity();

			services.MatchmakingService.OnGameMatched += _ => CancelAllInvites();
			services.MatchmakingService.IsMatchmaking.Observe((_, _) =>
			{
				UpdateCurrentPlayerActivity();
			});
			services.MatchmakingService.OnMatchmakingJoined += _ =>
			{
				CancelAllInvites();
			};
			services.RoomService.OnJoinedRoom += UpdateCurrentPlayerActivity;
			services.RoomService.OnLeaveRoom += UpdateCurrentPlayerActivity;
			services.MessageBrokerService.Subscribe<MainMenuOpenedMessage>(_ => UpdateCurrentPlayerActivity());
			services.UIService.OnScreenOpened += (screen, _) =>
			{
				UpdateCurrentPlayerActivity();
			};
			services.MessageBrokerService.Subscribe<CollectionItemEquippedMessage>(OnEquippedAvatar);
		}

		private void OnEquippedAvatar(CollectionItemEquippedMessage msg)
		{
			if (msg.Category != CollectionCategories.PROFILE_PICTURE) return;
			var config = _services.ConfigsProvider.GetConfig<AvatarCollectableConfig>();
			var url = AvatarHelpers.GetAvatarUrl(msg.EquippedItem, config);
			CloudSaveService.Instance.SaveAvatarURLAsync(url).Forget();
		}

		private void OnJoinedParty()
		{
			var mm = MainInstaller.ResolveServices().MatchmakingService;
			if (mm.IsMatchmaking.Value)
			{
				mm.LeaveMatchmaking();
			}

			UpdateCurrentPlayerActivity();
		}

		private void CancelAllInvites()
		{
			if (_services.UIService.IsScreenOpen<InvitePopupPresenter>())
			{
				_services.UIService.CloseScreen<InvitePopupPresenter>();
			}
		}

		private void UpdateCurrentPlayerActivity()
		{
			SetCurrentActivity(GetCurrentPlayerActivity());
		}

		public GameActivities GetCurrentPlayerActivity()
		{
			if (_services.RoomService.InRoom)
			{
				var spectating = _services.UIService.IsScreenOpen<SpectateScreenPresenter>();
				return spectating ? GameActivities.Spectating : GameActivities.In_match;
			}

			// If this is bindinded player is not in the main menu, dirty ugly hack
			if (MainInstaller.TryResolve<IMatchServices>(out _))
			{
				return GameActivities.In_match;
			}

			if (_services.MatchmakingService.IsMatchmaking.Value)
			{
				return GameActivities.In_matchmaking;
			}

			if (_services.FLLobbyService.IsInMatchLobby())
			{
				return GameActivities.In_game_lobby;
			}

			if (_services.FLLobbyService.IsInPartyLobby() && _services.FLLobbyService.HasTeamMembers())
			{
				return GameActivities.In_team;
			}

			return GameActivities.In_main_menu;
		}

		public bool CanAddFriend(Player player)
		{
			var isFriend = FriendsService.Instance.GetFriendByID(player.Id) != null;
			var isPending = FriendsService.Instance.OutgoingFriendRequests.Any(r => r.Member.Id == player.Id);
			return !isFriend && !isPending;
		}

		public bool CanInvite(Relationship friend, out string reason)
		{
			if (!friend.IsOnline())
			{
				reason = "player_offline";
				return false;
			}

			var activity = friend.Member?.Presence?.GetActivity<FriendActivity>();
			if (activity == null)
			{
				reason = "player_offline";
				return false;
			}

			if (!activity.CurrentActivityEnum.CanReceiveInvite())
			{
				reason = "activity_" + activity.CurrentActivityEnum.ToString().ToLowerInvariant();
				return false;
			}

			if (activity.Region != _services.LocalPrefsService.ServerRegion.Value)
			{
				reason = "different_region";
				return false;
			}

			if (_services.FLLobbyService.CurrentMatchLobby != null && _services.FLLobbyService.SentMatchInvites.Contains(friend.Member.Id))
			{
				reason = "already_invited_to_match";
				return false;
			}

			if (_services.FLLobbyService.SentPartyInvites.Any(sent => sent.PlayerId == friend.Member.Id))
			{
				reason = "already_invited_to_team";
				return false;
			}

			if (_services.FLLobbyService.CurrentPartyLobby != null)
			{
				if (_services.FLLobbyService.CurrentPartyLobby.Players.Any(p => p.Id == friend.Member.Id))
				{
					reason = "already_member";
					return false;
				}
				
				if (_services.FLLobbyService.CurrentPartyLobby.Players.Count > 4)
				{
					reason = "team_full";
					return false;
				}
			}

			reason = null;
			return true;
		}

		public void SetCurrentActivity(GameActivities activity)
		{
			_stateUpdates.Add(() =>
			{
				var data = MainInstaller.ResolveData();
				_playerActivity.CurrentActivity = (int) activity;
				_playerActivity.AvatarUrl = data.CollectionDataProvider.GetEquippedAvatarUrl();
				_playerActivity.Region = _services.LocalPrefsService.ServerRegion.Value;
				_playerActivity.Trophies = (int) data.PlayerDataProvider.Trophies.Value;
				FLog.Verbose("Setting social activity as " + JsonConvert.SerializeObject(_playerActivity));
				FriendsService.Instance.SetPresenceAsync(Availability.Online, _playerActivity).AsUniTask().Forget();
			});
		}

		public async UniTask FakeInviteBot(string botName)
		{
			_fakeBotRequests.Add(botName);
			await UniTask.Delay(124);
			_services.NotificationService.QueueNotification("Friend request sent");
		}

		public bool IsBotInvited(string botName)
		{
			return _fakeBotRequests.Contains(botName);
		}

		private void AddForBots(string playerName, List<PlayerContextButton> buttons)
		{
			buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, ScriptLocalization.UITFriends.option_open_profile,
				() => PlayerStatisticsPopupPresenter.OpenBot(playerName).Forget()));

			var canUseFriendSystem = _dataProvider.PlayerDataProvider.HasUnlocked(UnlockSystem.Friends);

			if (!canUseFriendSystem) return;
			if (!IsBotInvited(playerName))
			{
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, ScriptLocalization.UITFriends.option_send_request,
					() => FakeInviteBot(playerName).Forget()));
			}
			else
			{
				buttons.Add(PlayerContextButton.Create(ScriptLocalization.UITFriends.option_request_sent).Disable());
			}
		}

		private void AddOpenProfileAndFriendsOptions(string playerName, string unityId, List<PlayerContextButton> buttons, PlayerContextSettings settings)
		{
			if (buttons == null) buttons = new List<PlayerContextButton>();
			if (unityId == null) // bot
			{
				AddForBots(playerName, buttons);
				return;
			}

			var relationship = FriendsService.Instance.GetRelationShipById(unityId);
			buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, ScriptLocalization.UITFriends.option_open_profile,
				() => PlayerStatisticsPopupPresenter.Open(unityId).Forget()));

			// Blocked
			if (relationship is {Type: RelationshipType.Block})
			{
				if (settings.ShowBlock)
				{
					buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Red, ScriptLocalization.UITFriends.option_unblock,
						() => FriendsService.Instance.UnblockHandled(relationship).ContinueWith(_ => settings.OnRelationShipChange?.Invoke()).Forget()));
				}

				return;
			}

			var hasIncomingRequest = relationship is {Type: RelationshipType.FriendRequest} && !relationship.IsOutgoingInvite();
			var canUseFriendSystem = _dataProvider.PlayerDataProvider.HasUnlocked(UnlockSystem.Friends);
			if ((relationship == null || hasIncomingRequest) && canUseFriendSystem)
			{
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, ScriptLocalization.UITFriends.option_send_request,
					() => FriendsService.Instance.AddFriendHandled(unityId).ContinueWith(_ => settings.OnRelationShipChange?.Invoke()).Forget()));
			}
			else if (relationship is {Type: RelationshipType.FriendRequest} && relationship.IsOutgoingInvite())
			{
				if (settings.ShowRemoveFriend)
				{
					buttons.Add(new PlayerContextButton(
						PlayerButtonContextStyle.Red,
						ScriptLocalization.UITFriends.option_cancel_invite,
						() => FriendsService.Instance.RemoveRelationshipHandled(relationship).ContinueWith(_ => settings.OnRelationShipChange?.Invoke()).Forget()
					));
				}
				else
				{
					buttons.Add(PlayerContextButton.Create(ScriptLocalization.UITFriends.option_request_sent).Disable());
				}
			}
			else if (relationship is {Type: RelationshipType.Friend} && settings.ShowRemoveFriend)
			{
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Red, ScriptLocalization.UITFriends.remove_friend,
					() => FriendsService.Instance.RemoveRelationshipHandled(relationship).ContinueWith(_ => settings.OnRelationShipChange?.Invoke()).Forget()));
			}

			if (settings.ShowBlock)
			{
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Red, ScriptLocalization.UITFriends.block,
					() => FriendsService.Instance.BlockHandled(unityId).ContinueWith(_ => settings.OnRelationShipChange?.Invoke()).Forget()));
			}
		}

		public void OpenPlayerOptions(VisualElement element, VisualElement root, string unityId, string playerName, PlayerContextSettings settings = null)
		{
			if (unityId == AuthenticationService.Instance.PlayerId)
			{
				element.OpenTooltip(root, ScriptLocalization.UITCustomGames.local_player_tooltip);
				return;
			}

			if (settings == null) settings = new PlayerContextSettings();
			var isLocalPlayerLeader = _services.FLLobbyService.CurrentPartyLobby?.IsLocalPlayerHost() ?? false;
			var playerContextButtons = new List<PlayerContextButton>();

			if (isLocalPlayerLeader && settings.ShowTeamOptions)
			{
				playerContextButtons.Add(new PlayerContextButton
				{
					Text = ScriptLocalization.UITParty.option_promote,
					ContextStyle = PlayerButtonContextStyle.Gold,
					OnClick = UniTask.Action(async () => await _services.FLLobbyService.UpdatePartyHost(unityId))
				});
			}

			AddOpenProfileAndFriendsOptions(playerName, unityId, playerContextButtons, settings);
			if (isLocalPlayerLeader && settings.ShowTeamOptions)
			{
				playerContextButtons.Add(new PlayerContextButton
					{
						ContextStyle = PlayerButtonContextStyle.Red,
						Text = ScriptLocalization.UITParty.option_kick,
						OnClick = UniTask.Action(async () => await _services.FLLobbyService.KickPlayerFromParty(unityId))
					}
				);
			}

			if (settings.ExtraButtons != null)
			{
				playerContextButtons.AddRange(settings.ExtraButtons);
			}

			var displayName = playerName;
			// TODO Add support for trophies
			// var trophies = partyMember.GetPlayerTrophies();
			// displayName += $"\n{trophies} <sprite name=\"TrophyIcon\">";
			TooltipUtils.OpenPlayerContextOptions(element, root, displayName, playerContextButtons, settings.Position);
		}
	}
}