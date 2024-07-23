using System;
using System.Linq;
using System.Runtime.Serialization;
using Best.HTTP.Shared.PlatformSupport.IL2CPP;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using Quantum;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;

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
		public string Status => CurrentActivityEnum.ToString().Replace(@"_", " ");
		public GameActivities CurrentActivityEnum => ((GameActivities) CurrentActivity);
	}

	public interface IGameSocialService
	{
		bool CanInvite(Relationship friend);
		void SetCurrentActivity(GameActivities activity);
	}

	public class GameSocialService : IGameSocialService
	{
		private IGameServices _services;
		public GameSocialService(IGameServices services)
		{
			services.FLLobbyService.CurrentPartyCallbacks.LobbyJoined += _ => OnJoinedParty();
			services.MatchmakingService.OnGameMatched += _ => CancelAllInvites();
			services.MatchmakingService.OnMatchmakingJoined += _ => SetCurrentActivity(GameActivities.In_Matchmaking);
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
		}

		private void CancelAllInvites()
		{
			if (_services.UIService.IsScreenOpen<InvitePopupPresenter>())
			{
				_services.UIService.CloseScreen<InvitePopupPresenter>();
			}
		}

		private bool IsCustomGame => _services.RoomService.CurrentRoom?.Properties?.SimulationMatchConfig?.Value?.MatchType == MatchType.Custom;

		private bool IsInMenu  {
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
			} else if (service.IsScreenOpen<CollectionScreenPresenter>())
			{
				SetCurrentActivity(GameActivities.In_Collection);
			} else if (service.IsScreenOpen<FriendsScreenPresenter>())
			{
				SetCurrentActivity(GameActivities.In_Friends_Screen);
			} else if (service.IsScreenOpen<PreGameLoadingScreenPresenter>())
			{
				SetCurrentActivity(GameActivities.In_a_Match);
			} else if (service.IsScreenOpen<MatchLobbyScreenPresenter>())
			{
				SetCurrentActivity(GameActivities.In_Game_Lobby);
			} else if (service.IsScreenOpen<HomeScreenPresenter>() && !services.RoomService.InRoom && services.FLLobbyService.CurrentMatchLobby == null)
			{
				SetCurrentActivity(GameActivities.In_Main_Menu);
			}
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
			if (_services.FLLobbyService.CurrentMatchLobby != null && _services.FLLobbyService.SentMatchInvites.Contains(friend.Member.Id))
			{
				return false;
			}
			if (_services.FLLobbyService.CurrentPartyLobby != null)
			{
				if(_services.FLLobbyService.SentPartyInvites.Contains(friend.Member.Id)) return false;
				if (_services.FLLobbyService.CurrentPartyLobby.Players.Any(p => p.Id == friend.Member.Id)) return false;
			}
			/*
			if (_services.FLLobbyService.CurrentPartyLobby == null && _services.FLLobbyService.CurrentMatchLobby == null)
			{
				return false;
			}
			*/
			return true;
		}

		private void OnScreenOpened(string name, string layer)
		{
			DecideBasedOnScreen();
		}
		
		public void SetCurrentActivity(GameActivities activity)
		{
			FLog.Verbose("Setting social activity as "+activity);
			FriendsService.Instance.SetPresenceAsync(Availability.Online, new FriendActivity
			{
				CurrentActivity = (int)activity,
				AvatarUrl = MainInstaller.ResolveData().AppDataProvider.AvatarUrl
			}).AsUniTask().Forget();
		}
	}
}