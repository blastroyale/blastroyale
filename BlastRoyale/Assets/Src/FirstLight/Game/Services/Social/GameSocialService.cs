using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Best.HTTP.Shared.PlatformSupport.IL2CPP;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using I2.Loc;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Quantum;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Lobbies.Models;

namespace FirstLight.Game.Services
{
	public enum GameActivities
	{
		In_Main_Menu,
		In_Game_Lobby,
		In_a_Match,
		In_Matchmaking,
		In_Shop,
		In_Collection,
		In_Blast_Pass,
		In_Friends_Screen,
	}

	[DataContract, Preserve]
	public class FriendActivity
	{
		[Preserve, DataMember(Name = "status", IsRequired = true, EmitDefaultValue = true)]
		public int CurrentActivity { get; set; }

		[Preserve, DataMember(Name = "avatar", IsRequired = true, EmitDefaultValue = true)]
		public string AvatarUrl { get; set; }

		[Preserve, DataMember(Name = "team", IsRequired = false, EmitDefaultValue = true), CanBeNull]
		public string TeamId { get; set; }

		public string Status => CurrentActivityEnum.ToString().Replace(@"_", " ");
		public GameActivities CurrentActivityEnum => ((GameActivities) CurrentActivity);
	}

	public interface IGameSocialService
	{
		bool CanInvite(Relationship friend);
		bool CanAddFriend(Player friend);
		void SetCurrentActivity(GameActivities activity);

		UniTask FakeInviteBot(string botName);
		bool IsBotInvited(string botName);
		List<PlayerContextButton> AddDefaultPlayerOptions(string playerName, string unityId, List<PlayerContextButton> buttons = null);
	}

	public class GameSocialService : IGameSocialService
	{
		private BufferedQueue _stateUpdates = new ();
		private IGameServices _services;
		private HashSet<string> _fakeBotRequests = new ();
		private HashSet<string> _fakeBlocks = new ();
		private FriendActivity _playerActivity = new ();
		
		public GameSocialService(IGameServices services)
		{
			services.FLLobbyService.CurrentPartyCallbacks.LobbyDeleted += DecideBasedOnScreen;
			services.FLLobbyService.CurrentPartyCallbacks.KickedFromLobby += DecideBasedOnScreen;
			services.FLLobbyService.CurrentPartyCallbacks.LobbyJoined += _ => OnJoinedParty();
			services.MatchmakingService.OnGameMatched += _ => CancelAllInvites();
			services.MatchmakingService.OnMatchmakingJoined += _ => 
			{
				CancelAllInvites();
				SetCurrentActivity(GameActivities.In_Matchmaking);
			};
			services.MatchmakingService.OnMatchmakingCancelled += DecideBasedOnScreen;
			services.MessageBrokerService.Subscribe<MainMenuOpenedMessage>(_ => DecideBasedOnScreen());
			services.MessageBrokerService.Subscribe<JoinRoomMessage>(_ => SetCurrentActivity(
				IsCustomGame ? GameActivities.In_Game_Lobby : GameActivities.In_a_Match));
			services.MessageBrokerService.Subscribe<MatchStartedMessage>(_ => SetCurrentActivity(GameActivities.In_a_Match));
			services.MessageBrokerService.Subscribe<ShopScreenOpenedMessage>(_ => SetCurrentActivity(GameActivities.In_Shop));
			services.UIService.OnScreenOpened += OnScreenOpened;
			_services = services;
			_stateUpdates.OnlyKeepLast = true;
		}

		private void OnJoinedParty()
		{
			var mm = MainInstaller.ResolveServices().MatchmakingService;
			if (mm.IsMatchmaking.Value)
			{
				mm.LeaveMatchmaking();
			}
			DecideBasedOnScreen();
		}
		
		private void CancelAllInvites()
		{
			if (_services.UIService.IsScreenOpen<InvitePopupPresenter>())
			{
				_services.UIService.CloseScreen<InvitePopupPresenter>();
			}
		}

		private bool IsCustomGame => _services.RoomService.CurrentRoom?.Properties?.SimulationMatchConfig?.Value?.MatchType == MatchType.Custom;

		private bool IsInMenu
		{
			get
			{
				var services = MainInstaller.ResolveServices();
				return services.UIService.IsScreenOpen<HomeScreenPresenter>() && !services.RoomService.InRoom &&
					services.FLLobbyService.CurrentMatchLobby == null;
			}
		}

		private void DecideBasedOnScreen()
		{
			var services = MainInstaller.ResolveServices();
			var service = MainInstaller.ResolveServices().UIService;
			if (service.IsScreenOpen<BattlePassScreenPresenter>())
			{
				SetCurrentActivity(GameActivities.In_Blast_Pass);
			}
			else if (service.IsScreenOpen<CollectionScreenPresenter>())
			{
				SetCurrentActivity(GameActivities.In_Collection);
			}
			else if (service.IsScreenOpen<FriendsScreenPresenter>())
			{
				SetCurrentActivity(GameActivities.In_Friends_Screen);
			}
			else if (service.IsScreenOpen<PreGameLoadingScreenPresenter>())
			{
				SetCurrentActivity(GameActivities.In_a_Match);
			}
			else if (service.IsScreenOpen<MatchLobbyScreenPresenter>())
			{
				SetCurrentActivity(GameActivities.In_Game_Lobby);
			}
			else if (service.IsScreenOpen<HomeScreenPresenter>() && !services.RoomService.InRoom && services.FLLobbyService.CurrentMatchLobby == null)
			{
				SetCurrentActivity(GameActivities.In_Main_Menu);
			}
		}

		public bool CanAddFriend(Player player)
		{
			var isFriend = FriendsService.Instance.GetFriendByID(player.Id) != null;
			var isPending = FriendsService.Instance.OutgoingFriendRequests.Any(r => r.Member.Id == player.Id);
			return !isFriend && !isPending;
		}

		public bool CanInvite(Relationship friend)
		{
			if (!friend.IsOnline()) return false;

			var activity = friend.Member?.Presence?.GetActivity<FriendActivity>();
			if (activity == null) return false;

			if (activity.CurrentActivityEnum == GameActivities.In_a_Match || activity.CurrentActivityEnum == GameActivities.In_Matchmaking)
			{
				return false;
			}

			if (!string.IsNullOrEmpty(activity.TeamId))
			{
				return false;
			}

			if (_services.FLLobbyService.CurrentMatchLobby != null && _services.FLLobbyService.SentMatchInvites.Contains(friend.Member.Id))
			{
				return false;
			}

			if (_services.FLLobbyService.CurrentPartyLobby != null)
			{
				if (_services.FLLobbyService.SentPartyInvites.Contains(friend.Member.Id)) return false;
				if (_services.FLLobbyService.CurrentPartyLobby.Players.Any(p => p.Id == friend.Member.Id)) return false;
			}

			return true;
		}

		private void OnScreenOpened(string name, string layer)
		{
			DecideBasedOnScreen();
		}

		public void SetCurrentActivity(GameActivities activity)
		{
			_stateUpdates.Add(() =>
			{
				_playerActivity.CurrentActivity = (int) activity;
				_playerActivity.AvatarUrl = MainInstaller.ResolveData().AppDataProvider.AvatarUrl;
				_playerActivity.TeamId = _services.FLLobbyService.CurrentPartyLobby?.Id;
				FLog.Verbose("Setting social activity as " + JsonConvert.SerializeObject(_playerActivity));
				FriendsService.Instance.SetPresenceAsync(Availability.Online,_playerActivity ).AsUniTask().Forget();
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
			buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, "Open profile",
				() => PlayerStatisticsPopupPresenter.OpenBot(playerName).Forget()));

			if (_fakeBlocks.Contains(playerName))
			{
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Red, "Unblock",
					() =>
					{
						_fakeBlocks.Remove(playerName);
						_services.NotificationService.QueueNotification("#Player unblocked#");

					}));
			}
			else
			{
				if (!IsBotInvited(playerName))
				{
					buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, "Send friend request",
						() => this.FakeInviteBot(playerName).Forget()));
				}
				else
				{
					buttons.Add(PlayerContextButton.Create("Request sent").Disable());
				}

				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Red, "Block",
					() =>
					{
						_fakeBlocks.Add(playerName);
						_services.NotificationService.QueueNotification("#Player blocked#");
					}));
			}
		}

		public List<PlayerContextButton> AddDefaultPlayerOptions(string playerName, string unityId, List<PlayerContextButton> buttons)
		{
			if (buttons == null) buttons = new List<PlayerContextButton>();
			if (unityId == null) // bot
			{
				AddForBots(playerName, buttons);
				return buttons;
			}

			var relationship = FriendsService.Instance.GetRelationShipById(unityId);
			buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, "Open profile",
				() => PlayerStatisticsPopupPresenter.Open(unityId).Forget()));

			if (relationship is {Type: RelationshipType.Block})
			{
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Red, "Unblock",
					() => FriendsService.Instance.UnblockHandled(relationship).Forget()));
			}
			else
			{
				if (relationship == null)
				{
					buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, "Send friend request",
						() => FriendsService.Instance.AddFriendHandled(unityId).Forget()));
				}
				else if (relationship.Type == RelationshipType.FriendRequest)
				{
					buttons.Add(PlayerContextButton.Create("Request sent").Disable());
				}

				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Red, "Block",
					() => FriendsService.Instance.BlockHandled(unityId, false).Forget()));
			}

			return buttons;
		}
	}
}