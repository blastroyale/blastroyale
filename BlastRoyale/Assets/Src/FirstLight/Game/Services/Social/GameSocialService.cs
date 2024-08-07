using System;
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
using UnityEngine.UIElements;
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
		bool CanInvite(Relationship friend);
		bool CanAddFriend(Player friend);
		void SetCurrentActivity(GameActivities activity);

		UniTask FakeInviteBot(string botName);
		bool IsBotInvited(string botName);
		public void OpenPlayerOptions(VisualElement element, VisualElement root, string unityId, string playerName, PlayerContextSettings settings = null);
	}

	public class GameSocialService : IGameSocialService
	{
		private BufferedQueue _stateUpdates = new (TimeSpan.FromSeconds(3), true);
		private IGameServices _services;
		private HashSet<string> _fakeBotRequests = new ();
		private FriendActivity _playerActivity = new ();

		public GameSocialService(IGameServices services)
		{
			services.FLLobbyService.CurrentPartyCallbacks.LobbyDeleted += DecideBasedOnScreen;
			services.FLLobbyService.CurrentPartyCallbacks.KickedFromLobby += DecideBasedOnScreen;
			services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyJoined += _ => OnJoinedParty();
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

			if (activity.Region != _services.LocalPrefsService.ServerRegion.Value)
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
				if (_services.FLLobbyService.SentPartyInvites.Any(sent => sent.PlayerId == friend.Member.Id)) return false;
				if (_services.FLLobbyService.CurrentPartyLobby.Players.Any(p => p.Id == friend.Member.Id)) return false;
				if (_services.FLLobbyService.CurrentPartyLobby.Players.Count >= _services.FLLobbyService.CurrentPartyLobby.MaxPlayers) return false;
			}

			return true;
		}

		private void OnScreenOpened(string name, string layer)
		{
			if (name.Contains("Popup") || name.Contains("Notification")) return;
			DecideBasedOnScreen();
		}

		public void SetCurrentActivity(GameActivities activity)
		{
			_stateUpdates.Add(() =>
			{
				var data = MainInstaller.ResolveData();
				_playerActivity.CurrentActivity = (int) activity;
				_playerActivity.AvatarUrl = data.AppDataProvider.AvatarUrl;
				_playerActivity.TeamId = _services.FLLobbyService.CurrentPartyLobby?.Id;
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

			if (relationship == null || relationship.Type == RelationshipType.FriendRequest && !relationship.IsOutgoingInvite())
			{
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, ScriptLocalization.UITFriends.option_send_request,
					() => FriendsService.Instance.AddFriendHandled(unityId).ContinueWith(_ => settings.OnRelationShipChange?.Invoke()).Forget()));
			}
			else if (relationship.Type == RelationshipType.FriendRequest && relationship.IsOutgoingInvite())
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
			else if (relationship.Type == RelationshipType.Friend && settings.ShowRemoveFriend)
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