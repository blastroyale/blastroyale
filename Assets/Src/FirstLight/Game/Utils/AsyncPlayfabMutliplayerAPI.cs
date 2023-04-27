using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.MultiplayerModels;

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

		private static Func<UpdateLobbyRequest, Task<LobbyEmptyResult>> UpdateLobbyFunc { get; }
			= Wrap<UpdateLobbyRequest, LobbyEmptyResult>(PlayFabMultiplayerAPI.UpdateLobby);

		/// <inheritdoc cref="PlayFabMultiplayerAPI.UpdateLobby"/>
		public static Task<LobbyEmptyResult> UpdateLobby(UpdateLobbyRequest req)
		{
			return UpdateLobbyFunc(req);
		}


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