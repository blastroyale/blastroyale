using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
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


		private static Func<TRequest, UniTask<TResponse>> Wrap<TRequest, TResponse>(Action<TRequest, Action<TResponse>, Action<PlayFabError>, object, Dictionary<string, string>> func)
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