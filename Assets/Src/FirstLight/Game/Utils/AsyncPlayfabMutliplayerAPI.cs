using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.MultiplayerModels;
using UnityEngine.UIElements;

namespace FirstLight.Game.Utils
{
	public static class AsyncPlayfabMultiplayerAPI
	{
		private static Func<SubscribeToLobbyResourceRequest, Task<SubscribeToLobbyResourceResult>> SubscribeToLobbyResourceFunc { get; }
			= Wrap<SubscribeToLobbyResourceRequest, SubscribeToLobbyResourceResult>(PlayFabMultiplayerAPI.SubscribeToLobbyResource);

		private static Func<UnsubscribeFromLobbyResourceRequest, Task<LobbyEmptyResult>> UnsubscribeFromLobbyResourceFunc { get; }
			= Wrap<UnsubscribeFromLobbyResourceRequest, LobbyEmptyResult>(PlayFabMultiplayerAPI.UnsubscribeFromLobbyResource);

		private static Func<RemoveMemberFromLobbyRequest, Task<LobbyEmptyResult>> RemoveMemberFunc { get; }
			= Wrap<RemoveMemberFromLobbyRequest, LobbyEmptyResult>(PlayFabMultiplayerAPI.RemoveMember);

		private static Func<JoinLobbyRequest, Task<JoinLobbyResult>> JoinLobbyFunc { get; }
			= Wrap<JoinLobbyRequest, JoinLobbyResult>(PlayFabMultiplayerAPI.JoinLobby);

		private static Func<CreateLobbyRequest, Task<CreateLobbyResult>> CreateLobbyFunc { get; }
			= Wrap<CreateLobbyRequest, CreateLobbyResult>(PlayFabMultiplayerAPI.CreateLobby);

		private static Func<GetLobbyRequest, Task<GetLobbyResult>> GetLobbyFunc { get; }
			= Wrap<GetLobbyRequest, GetLobbyResult>(PlayFabMultiplayerAPI.GetLobby);

		private static Func<FindLobbiesRequest, Task<FindLobbiesResult>> FindLobbiesFunc { get; }
			= Wrap<FindLobbiesRequest, FindLobbiesResult>(PlayFabMultiplayerAPI.FindLobbies);

		private static Func<LeaveLobbyRequest, Task<LobbyEmptyResult>> LeaveLobbyFunc { get; }
			= Wrap<LeaveLobbyRequest, LobbyEmptyResult>(PlayFabMultiplayerAPI.LeaveLobby);

		/// <inheritdoc cref="PlayFabMultiplayerAPI.LeaveLobby"/>
		public static Task<LobbyEmptyResult> LeaveLobby(LeaveLobbyRequest req)
		{
			return LeaveLobbyFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.FindLobbies"/>
		public static Task<FindLobbiesResult> FindLobbies(FindLobbiesRequest req)
		{
			return FindLobbiesFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.GetLobby"/>
		public static Task<GetLobbyResult> GetLobby(GetLobbyRequest req)
		{
			return GetLobbyFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.CreateLobby"/>
		public static Task<CreateLobbyResult> CreateLobby(CreateLobbyRequest req)
		{
			return CreateLobbyFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.JoinLobby"/>
		public static Task<JoinLobbyResult> JoinLobby(JoinLobbyRequest req)
		{
			return JoinLobbyFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.RemoveMember"/>
		public static Task<LobbyEmptyResult> RemoveMember(RemoveMemberFromLobbyRequest req)
		{
			return RemoveMemberFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.UnsubscribeFromLobbyResource"/>
		public static Task<LobbyEmptyResult> UnsubscribeFromLobbyResource(UnsubscribeFromLobbyResourceRequest req)
		{
			return UnsubscribeFromLobbyResourceFunc(req);
		}

		/// <inheritdoc cref="PlayFabMultiplayerAPI.SubscribeToLobbyResource"/>
		public static Task<SubscribeToLobbyResourceResult> SubscribeToLobbyResource(SubscribeToLobbyResourceRequest req)
		{
			return SubscribeToLobbyResourceFunc(req);
		}


		private static Func<TRequest, Task<TResponse>> Wrap<TRequest, TResponse>(Action<TRequest, Action<TResponse>, Action<PlayFabError>, object, Dictionary<string, string>> func)
		{
			return request =>
			{
				var t = new TaskCompletionSource<TResponse>();
				func(request, r => t.TrySetResult(r), e => { t.TrySetException(new WrappedPlayFabException(e)); }, default, default);
				return t.Task;
			};
		}
	}

	public class WrappedPlayFabException : Exception
	{
		public PlayFabError Error { get; }

		public WrappedPlayFabException(PlayFabError error) : base(error.ErrorMessage)
		{
			Error = error;
		}
	}
}