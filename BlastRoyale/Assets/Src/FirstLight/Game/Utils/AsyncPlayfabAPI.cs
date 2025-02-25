using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using PlayFab.MultiplayerModels;

namespace FirstLight.Game.Utils
{
	public static class AsyncPlayfabAPI
	{
		private static Func<SubscribeToLobbyResourceRequest, UniTask<SubscribeToLobbyResourceResult>> SubscribeToLobbyResourceFunc { get; }
			= Wrap<SubscribeToLobbyResourceRequest, SubscribeToLobbyResourceResult>(PlayFabMultiplayerAPI.SubscribeToLobbyResource);

		private static Func<UnsubscribeFromLobbyResourceRequest, UniTask<LobbyEmptyResult>> UnsubscribeFromLobbyResourceFunc { get; }
			= Wrap<UnsubscribeFromLobbyResourceRequest, LobbyEmptyResult>(PlayFabMultiplayerAPI.UnsubscribeFromLobbyResource);

		private static Func<RemoveMemberFromLobbyRequest, UniTask<LobbyEmptyResult>> RemoveMemberFunc { get; }
			= Wrap<RemoveMemberFromLobbyRequest, LobbyEmptyResult>(PlayFabMultiplayerAPI.RemoveMember);

		private static Func<JoinLobbyRequest, UniTask<JoinLobbyResult>> JoinLobbyFunc { get; }
			= Wrap<JoinLobbyRequest, JoinLobbyResult>(PlayFabMultiplayerAPI.JoinLobby);

		private static Func<CreateLobbyRequest, UniTask<CreateLobbyResult>> CreateLobbyFunc { get; }
			= Wrap<CreateLobbyRequest, CreateLobbyResult>(PlayFabMultiplayerAPI.CreateLobby);

		private static Func<GetLobbyRequest, UniTask<GetLobbyResult>> GetLobbyFunc { get; }
			= Wrap<GetLobbyRequest, GetLobbyResult>(PlayFabMultiplayerAPI.GetLobby);

		private static Func<FindLobbiesRequest, UniTask<FindLobbiesResult>> FindLobbiesFunc { get; }
			= Wrap<FindLobbiesRequest, FindLobbiesResult>(PlayFabMultiplayerAPI.FindLobbies);

		private static Func<LeaveLobbyRequest, UniTask<LobbyEmptyResult>> LeaveLobbyFunc { get; }
			= Wrap<LeaveLobbyRequest, LobbyEmptyResult>(PlayFabMultiplayerAPI.LeaveLobby);

		private static Func<UpdateLobbyRequest, UniTask<LobbyEmptyResult>> UpdateLobbyFunc { get; }
			= Wrap<UpdateLobbyRequest, LobbyEmptyResult>(PlayFabMultiplayerAPI.UpdateLobby);

		private static Func<GetTitleNewsRequest, UniTask<GetTitleNewsResult>> GetNewsFunc { get; }
			= Wrap<GetTitleNewsRequest, GetTitleNewsResult>(PlayFabClientAPI.GetTitleNews);

		private static Func<GetCatalogItemsRequest, UniTask<GetCatalogItemsResult>> GetCatalogItemsFunc { get; }
			= Wrap<GetCatalogItemsRequest, GetCatalogItemsResult>(PlayFabClientAPI.GetCatalogItems);

		private static Func<GetStoreItemsRequest, UniTask<GetStoreItemsResult>> GetStoreItemsFunc { get; }
			= Wrap<GetStoreItemsRequest, GetStoreItemsResult>(PlayFabClientAPI.GetStoreItems);

		private static Func<UpdateUserTitleDisplayNameRequest, UniTask<UpdateUserTitleDisplayNameResult>> UpdateUserTitleDisplayNameFunc { get; }
			= Wrap<UpdateUserTitleDisplayNameRequest, UpdateUserTitleDisplayNameResult>(PlayFabClientAPI.UpdateUserTitleDisplayName);

		private static Func<ExecuteFunctionRequest, UniTask<ExecuteFunctionResult>> ExecuteFunctionFunc { get; }
			= Wrap<ExecuteFunctionRequest, ExecuteFunctionResult>(PlayFabCloudScriptAPI.ExecuteFunction);

		/// <inheritdoc cref="PlayFabClientAPI.UpdateUserTitleDisplayName"/>
		public static UniTask<UpdateUserTitleDisplayNameResult> UpdateUserTitleDisplayName(UpdateUserTitleDisplayNameRequest req)
		{
			return UpdateUserTitleDisplayNameFunc(req);
		}

		public static UniTask<ExecuteFunctionResult> ExecuteFunction(ExecuteFunctionRequest req)
		{
			return ExecuteFunctionFunc(req);
		}

		/// <inheritdoc cref="PlayFabClientAPI.GetCatalogItems"/>
		public static UniTask<GetCatalogItemsResult> GetCatalogItems(GetCatalogItemsRequest req)
		{
			return GetCatalogItemsFunc(req);
		}

		/// <inheritdoc cref="PlayFabClientAPI.GetStoreItems"/>
		public static UniTask<GetStoreItemsResult> GetStoreItems(GetStoreItemsRequest req)
		{
			return GetStoreItemsFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.UpdateLobby"/>
		public static UniTask<LobbyEmptyResult> UpdateLobby(UpdateLobbyRequest req)
		{
			return UpdateLobbyFunc(req);
		}

		public static UniTask<GetTitleNewsResult> GetNews(GetTitleNewsRequest req)
		{
			return GetNewsFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.LeaveLobby"/>
		public static UniTask<LobbyEmptyResult> LeaveLobby(LeaveLobbyRequest req)
		{
			return LeaveLobbyFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.FindLobbies"/>
		public static UniTask<FindLobbiesResult> FindLobbies(FindLobbiesRequest req)
		{
			return FindLobbiesFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.GetLobby"/>
		public static UniTask<GetLobbyResult> GetLobby(GetLobbyRequest req)
		{
			return GetLobbyFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.CreateLobby"/>
		public static UniTask<CreateLobbyResult> CreateLobby(CreateLobbyRequest req)
		{
			return CreateLobbyFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.JoinLobby"/>
		public static UniTask<JoinLobbyResult> JoinLobby(JoinLobbyRequest req)
		{
			return JoinLobbyFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.RemoveMember"/>
		public static UniTask<LobbyEmptyResult> RemoveMember(RemoveMemberFromLobbyRequest req)
		{
			return RemoveMemberFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.UnsubscribeFromLobbyResource"/>
		public static UniTask<LobbyEmptyResult> UnsubscribeFromLobbyResource(UnsubscribeFromLobbyResourceRequest req)
		{
			return UnsubscribeFromLobbyResourceFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.SubscribeToLobbyResource"/>
		public static UniTask<SubscribeToLobbyResourceResult> SubscribeToLobbyResource(SubscribeToLobbyResourceRequest req)
		{
			return SubscribeToLobbyResourceFunc(req);
		}

		public static class MultiplayerAPI
		{
			private static Func<CreateMatchmakingTicketRequest, UniTask<CreateMatchmakingTicketResult>> CreateMatchmakingTicketFunc { get; }
				= Wrap<CreateMatchmakingTicketRequest, CreateMatchmakingTicketResult>(PlayFabMultiplayerAPI.CreateMatchmakingTicket);

			public static UniTask<CreateMatchmakingTicketResult> CreateMatchmakingTicket(
				CreateMatchmakingTicketRequest req)
			{
				return CreateMatchmakingTicketFunc(req);
			}

			private static Func<JoinMatchmakingTicketRequest, UniTask<JoinMatchmakingTicketResult>> JoinMatchmakingTicketFunc { get; }
				= Wrap<JoinMatchmakingTicketRequest, JoinMatchmakingTicketResult>(PlayFabMultiplayerAPI.JoinMatchmakingTicket);

			private static Func<CancelAllMatchmakingTicketsForPlayerRequest, UniTask<CancelAllMatchmakingTicketsForPlayerResult>>
				CancelAllMatchmakingTicketsForPlayerFunc { get; }
				= Wrap<CancelAllMatchmakingTicketsForPlayerRequest, CancelAllMatchmakingTicketsForPlayerResult>(PlayFabMultiplayerAPI
					.CancelAllMatchmakingTicketsForPlayer);

			private static Func<GetMatchmakingTicketRequest, UniTask<GetMatchmakingTicketResult>>
				GetMatchmakingTicketFunc { get; }
				= Wrap<GetMatchmakingTicketRequest, GetMatchmakingTicketResult>(PlayFabMultiplayerAPI
					.GetMatchmakingTicket);

			private static Func<GetMatchRequest, UniTask<GetMatchResult>>
				GetMatchFunc { get; }
				= Wrap<GetMatchRequest, GetMatchResult>(PlayFabMultiplayerAPI
					.GetMatch);

			public static UniTask<GetMatchResult> GetMatch(GetMatchRequest req)
			{
				return GetMatchFunc(req);
			}

			public static UniTask<GetMatchmakingTicketResult> GetMatchmakingTicket(GetMatchmakingTicketRequest req)
			{
				return GetMatchmakingTicketFunc(req);
			}

			public static UniTask<CancelAllMatchmakingTicketsForPlayerResult> CancelAllMatchmakingTicketsForPlayer(
				CancelAllMatchmakingTicketsForPlayerRequest req)
			{
				return CancelAllMatchmakingTicketsForPlayerFunc(req);
			}

			public static UniTask<JoinMatchmakingTicketResult> JoinMatchmakingTicket(
				JoinMatchmakingTicketRequest req)
			{
				return JoinMatchmakingTicketFunc(req);
			}
		}

		public static class ClientAPI
		{
			private static Func<GetPhotonAuthenticationTokenRequest, UniTask<GetPhotonAuthenticationTokenResult>> GetPhotonAuthenticationTokenFunc
			{
				get;
			}
				= Wrap<GetPhotonAuthenticationTokenRequest, GetPhotonAuthenticationTokenResult>(PlayFabClientAPI.GetPhotonAuthenticationToken);

			public static UniTask<GetPhotonAuthenticationTokenResult> GetPhotonAuthenticationToken(
				GetPhotonAuthenticationTokenRequest req)
			{
				return GetPhotonAuthenticationTokenFunc(req);
			}

			private static Func<GetUserDataRequest, UniTask<GetUserDataResult>> GetUserReadOnlyDataFunc { get; }
				= Wrap<GetUserDataRequest, GetUserDataResult>(PlayFabClientAPI.GetUserReadOnlyData);

			public static UniTask<GetUserDataResult> GetUserReadOnlyData(
				GetUserDataRequest req)
			{
				return GetUserReadOnlyDataFunc(req);
			}

			private static Func<LoginWithCustomIDRequest, UniTask<LoginResult>> LoginCustomIdFunc { get; }
				= Wrap<LoginWithCustomIDRequest, LoginResult>(PlayFabClientAPI.LoginWithCustomID);

			/// <inheritdoc cref="PlayFabClientAPI.LoginWithCustomID"/>
			public static UniTask<LoginResult> LoginWithCustomID(LoginWithCustomIDRequest r)
			{
				return LoginCustomIdFunc(r);
			}

			private static Func<LoginWithAndroidDeviceIDRequest, UniTask<LoginResult>> LoginWithAndroidDeviceIDFunc { get; }
				= Wrap<LoginWithAndroidDeviceIDRequest, LoginResult>(PlayFabClientAPI.LoginWithAndroidDeviceID);

			/// <inheritdoc cref="PlayFabClientAPI.LoginWithCustomID"/>
			public static UniTask<LoginResult> LoginWithAndroidDeviceID(LoginWithAndroidDeviceIDRequest r)
			{
				return LoginWithAndroidDeviceIDFunc(r);
			}

			private static Func<LoginWithIOSDeviceIDRequest, UniTask<LoginResult>> LoginWithIOSDeviceIDFunc { get; }
				= Wrap<LoginWithIOSDeviceIDRequest, LoginResult>(PlayFabClientAPI.LoginWithIOSDeviceID);

			/// <inheritdoc cref="PlayFabClientAPI.LoginWithCustomID"/>
			public static UniTask<LoginResult> LoginWithIOSDeviceID(LoginWithIOSDeviceIDRequest r)
			{
				return LoginWithIOSDeviceIDFunc(r);
			}

			private static Func<LinkCustomIDRequest, UniTask<LinkCustomIDResult>> LinkCustomIDFunc { get; }
				= Wrap<LinkCustomIDRequest, LinkCustomIDResult>(PlayFabClientAPI.LinkCustomID);

			/// <inheritdoc cref="PlayFabClientAPI.LinkCustomID"/>
			public static UniTask<LinkCustomIDResult> LinkCustomID(LinkCustomIDRequest r)
			{
				return LinkCustomIDFunc(r);
			}

			private static Func<LinkAndroidDeviceIDRequest, UniTask<LinkAndroidDeviceIDResult>> LinkAndroidDeviceIDFunc { get; }
				= Wrap<LinkAndroidDeviceIDRequest, LinkAndroidDeviceIDResult>(PlayFabClientAPI.LinkAndroidDeviceID);

			/// <inheritdoc cref="PlayFabClientAPI.LinkAndroidDeviceID"/>
			public static UniTask<LinkAndroidDeviceIDResult> LinkAndroidDeviceID(LinkAndroidDeviceIDRequest r)
			{
				return LinkAndroidDeviceIDFunc(r);
			}

			private static Func<LinkIOSDeviceIDRequest, UniTask<LinkIOSDeviceIDResult>> LinkIOSDeviceIDFunc { get; }
				= Wrap<LinkIOSDeviceIDRequest, LinkIOSDeviceIDResult>(PlayFabClientAPI.LinkIOSDeviceID);

			/// <inheritdoc cref="PlayFabClientAPI.LinkIOSDeviceID"/>
			public static UniTask<LinkIOSDeviceIDResult> LinkIOSDeviceID(LinkIOSDeviceIDRequest r)
			{
				return LinkIOSDeviceIDFunc(r);
			}

			private static Func<UnlinkCustomIDRequest, UniTask<UnlinkCustomIDResult>> UnlinkCustomIDFunc { get; }
				= Wrap<UnlinkCustomIDRequest, UnlinkCustomIDResult>(PlayFabClientAPI.UnlinkCustomID);

			/// <inheritdoc cref="PlayFabClientAPI.UnlinkCustomID"/>
			public static UniTask<UnlinkCustomIDResult> UnlinkCustomID(UnlinkCustomIDRequest r)
			{
				return UnlinkCustomIDFunc(r);
			}

			private static Func<UnlinkAndroidDeviceIDRequest, UniTask<UnlinkAndroidDeviceIDResult>> UnlinkAndroidDeviceIDFunc { get; }
				= Wrap<UnlinkAndroidDeviceIDRequest, UnlinkAndroidDeviceIDResult>(PlayFabClientAPI.UnlinkAndroidDeviceID);

			/// <inheritdoc cref="PlayFabClientAPI.UnlinkAndroidDeviceID"/>
			public static UniTask<UnlinkAndroidDeviceIDResult> UnlinkAndroidDeviceID(UnlinkAndroidDeviceIDRequest r)
			{
				return UnlinkAndroidDeviceIDFunc(r);
			}

			private static Func<UnlinkIOSDeviceIDRequest, UniTask<UnlinkIOSDeviceIDResult>> UnlinkIOSDeviceIDFunc { get; }
				= Wrap<UnlinkIOSDeviceIDRequest, UnlinkIOSDeviceIDResult>(PlayFabClientAPI.UnlinkIOSDeviceID);

			/// <inheritdoc cref="PlayFabClientAPI.UnlinkIOSDeviceID"/>
			public static UniTask<UnlinkIOSDeviceIDResult> UnlinkIOSDeviceID(UnlinkIOSDeviceIDRequest r)
			{
				return UnlinkIOSDeviceIDFunc(r);
			}

			private static Func<AddOrUpdateContactEmailRequest, UniTask<AddOrUpdateContactEmailResult>> AddOrUpdateContactEmailFunc { get; }
				= Wrap<AddOrUpdateContactEmailRequest, AddOrUpdateContactEmailResult>(PlayFabClientAPI.AddOrUpdateContactEmail);

			/// <inheritdoc cref="PlayFabClientAPI.AddOrUpdateContactEmail"/>
			public static UniTask<AddOrUpdateContactEmailResult> AddOrUpdateContactEmail(AddOrUpdateContactEmailRequest r)
			{
				return AddOrUpdateContactEmailFunc(r);
			}

			private static Func<AddUsernamePasswordRequest, UniTask<AddUsernamePasswordResult>> AddUsernamePasswordFunc { get; }
				= Wrap<AddUsernamePasswordRequest, AddUsernamePasswordResult>(PlayFabClientAPI.AddUsernamePassword);

			/// <inheritdoc cref="PlayFabClientAPI.AddOrUpdateContactEmail"/>
			public static UniTask<AddUsernamePasswordResult> AddUsernamePassword(AddUsernamePasswordRequest r)
			{
				return AddUsernamePasswordFunc(r);
			}

			private static Func<SendAccountRecoveryEmailRequest, UniTask<SendAccountRecoveryEmailResult>> SendAccountRecoveryEmailFunc { get; }
				= Wrap<SendAccountRecoveryEmailRequest, SendAccountRecoveryEmailResult>(PlayFabClientAPI.SendAccountRecoveryEmail);

			/// <inheritdoc cref="PlayFabClientAPI.SendAccountRecoveryEmail"/>
			public static UniTask<SendAccountRecoveryEmailResult> SendAccountRecoveryEmail(SendAccountRecoveryEmailRequest r)
			{
				return SendAccountRecoveryEmailFunc(r);
			}

			private static Func<LoginWithEmailAddressRequest, UniTask<LoginResult>> LoginWithEmailAddressFunc { get; }
				= Wrap<LoginWithEmailAddressRequest, LoginResult>(PlayFabClientAPI.LoginWithEmailAddress);

			/// <inheritdoc cref="PlayFabClientAPI.SendAccountRecoveryEmail"/>
			public static UniTask<LoginResult> LoginWithEmailAddress(LoginWithEmailAddressRequest r)
			{
				return LoginWithEmailAddressFunc(r);
			}

			private static Func<LoginWithGooglePlayGamesServicesRequest, UniTask<LoginResult>> LoginWithGooglePlayGamesServicesFunc { get; }
				= Wrap<LoginWithGooglePlayGamesServicesRequest, LoginResult>(PlayFabClientAPI.LoginWithGooglePlayGamesServices);

			/// <inheritdoc cref="PlayFabClientAPI.SendAccountRecoveryEmail"/>
			public static UniTask<LoginResult> LoginWithGooglePlayGamesServices(LoginWithGooglePlayGamesServicesRequest r)
			{
				return LoginWithGooglePlayGamesServicesFunc(r);
			}

			private static Func<LinkGooglePlayGamesServicesAccountRequest, UniTask<LinkGooglePlayGamesServicesAccountResult>>
				LinkGooglePlayGamesServicesAccountFunc { get; }
				= Wrap<LinkGooglePlayGamesServicesAccountRequest, LinkGooglePlayGamesServicesAccountResult>(PlayFabClientAPI
					.LinkGooglePlayGamesServicesAccount);

			/// <inheritdoc cref="PlayFabClientAPI.LinkGooglePlayGamesServicesAccount"/>
			public static UniTask<LinkGooglePlayGamesServicesAccountResult> LinkGooglePlayGamesServicesAccount(
				LinkGooglePlayGamesServicesAccountRequest r)
			{
				return LinkGooglePlayGamesServicesAccountFunc(r);
			}

			private static Func<LinkGameCenterAccountRequest, UniTask<LinkGameCenterAccountResult>> LinkGameCenterAccountFunc { get; }
				= Wrap<LinkGameCenterAccountRequest, LinkGameCenterAccountResult>(PlayFabClientAPI.LinkGameCenterAccount);

			/// <inheritdoc cref="PlayFabClientAPI.LinkGameCenterAccount"/>
			public static UniTask<LinkGameCenterAccountResult> LinkGameCenterAccount(LinkGameCenterAccountRequest r)
			{
				return LinkGameCenterAccountFunc(r);
			}

			private static Func<LoginWithGameCenterRequest, UniTask<LoginResult>> LoginWithGameCenterFunc { get; }
				= Wrap<LoginWithGameCenterRequest, LoginResult>(PlayFabClientAPI.LoginWithGameCenter);

			/// <inheritdoc cref="PlayFabClientAPI.LoginWithGameCenter"/>
			public static UniTask<LoginResult> LoginWithGameCenter(LoginWithGameCenterRequest r)
			{
				return LoginWithGameCenterFunc(r);
			}
		}

		private static Func<TRequest, UniTask<TResponse>> Wrap<TRequest, TResponse>(
			Action<TRequest, Action<TResponse>, Action<PlayFabError>, object, Dictionary<string, string>> func)
		{
			return request =>
			{
				var t = new UniTaskCompletionSource<TResponse>();
				func(request, r => t.TrySetResult(r), e => { t.TrySetException(new WrappedPlayFabException(e)); }, default, default);
				return t.Task;
			};
		}

		/// <summary>
		/// Convert a PlayFabApi error to an exception
		/// </summary>
		public static WrappedPlayFabException AsException(this PlayFabError error)
		{
			return new WrappedPlayFabException(error);
		}
	}

	public class WrappedPlayFabException : Exception
	{
		public PlayFabError Error { get; }

		private static string ErrorMessage(PlayFabError error)
		{
			if (error == null) return "";
			var str = new StringBuilder();
			if (string.IsNullOrEmpty(error.ErrorMessage))
			{
				str.Append("Unknown error message status code ");
				str.Append(error.HttpStatus);
			}
			else
			{
				str.Append(error.Error);
				str.Append(" - ");
				str.Append(error.ErrorMessage);
			}

			str.AppendLine();

			if (error.ApiEndpoint != null)
			{
				str.Append("At ").AppendLine(error.ApiEndpoint);
			}

			if (error.ErrorDetails == null || error.ErrorDetails.Count == 0) return str.ToString();

			foreach (var pair in error.ErrorDetails)
			{
				foreach (var msg in pair.Value)
				{
					str.AppendLine().Append(pair.Key).Append(": ").Append(msg);
				}
			}

			return str.ToString();
		}

		public WrappedPlayFabException(PlayFabError error) : base(ErrorMessage(error))
		{
			Error = error;
		}
	}
}