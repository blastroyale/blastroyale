using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;


namespace FirstLight.Game.Services.Social
{
	/// <summary>
	/// Represents the lobby grid logic
	/// Only host runs the logic
	/// </summary>
	public class LobbyGrid
	{
		private readonly AsyncBufferedQueue _gridUpdates = new (TimeSpan.FromSeconds(1f), true);

		public async UniTaskVoid HandleLobbyUpdates(Lobby lobby, ILobbyChanges changes)
		{
			var gridChanged = changes.Data.ChangeType != LobbyValueChangeType.Unchanged && changes.Data.Value.ContainsKey(FLLobbyService.KEY_LOBBY_MATCH_PLAYER_POSITIONS);
			var relevantChange = !changes.PlayerData.Changed || changes.PlayerData.Added;
			var spectatorChanged = false;
			var positionRequest = false;
			
			if (!relevantChange && changes.PlayerData.Changed)
			{
				foreach (var p in changes.PlayerData.Value.Values)
				{
					if (p.ChangedData.Added || p.ChangedData.Changed)
					{
						if (p.ChangedData.Value.TryGetValue(FLLobbyService.KEY_POSITION_REQUEST, out var posReq) && !posReq.Removed)
						{
							positionRequest = true;
							break;
						}
						if (p.ChangedData.Value.TryGetValue(FLLobbyService.KEY_SPECTATOR, out var spec) && !spec.Removed)
						{
							relevantChange = true;
							spectatorChanged = true;
							break;
						}
					}
				}
			}

			relevantChange = relevantChange || spectatorChanged || positionRequest;
			
			if (lobby.IsLocalPlayerHost() && !gridChanged && relevantChange)
			{
				EnqueueGridSync(lobby);
			}
			
			// only grid updates
			if (!changes.Data.Changed || !gridChanged) return;
			
			var localPlayer = lobby.Players.First(p => p.IsLocal());
			var expectedPosition = localPlayer.GetProperty(FLLobbyService.KEY_POSITION_REQUEST);
			if (string.IsNullOrEmpty(expectedPosition))
			{
				return;
			}
			var grid = lobby.GetPlayerGrid();
			var currentPosition = grid.GetPosition(AuthenticationService.Instance.PlayerId);

			if (currentPosition == Int32.Parse(expectedPosition))
			{
				FLog.Verbose("Cleaning position request, i reached my ultimate goal !!!");
				localPlayer.Data[FLLobbyService.KEY_POSITION_REQUEST] = null;
				await MainInstaller.ResolveServices().FLLobbyService.SetMatchPlayerProperty(FLLobbyService.KEY_POSITION_REQUEST, null);
			}
		}
		
		public void EnqueueGridSync(Lobby lobby)
		{
			if (lobby == null || !lobby.IsLocalPlayerHost()) return;
			
			_gridUpdates.Add(async () =>
			{
				var players = lobby.NonSpectators().Select(p => p.Id).ToHashSet();

				var grid = lobby.GetPlayerGrid();
				var gridHash = grid.PresentPlayers;
				gridHash.Remove("");

				var initialGridString = grid.ToString();
				var playersJoined = players.Except(gridHash).ToArray();
				var playersLeft = gridHash.Except(players).ToArray();
				
				FLog.Verbose($"Grid={grid} Players={string.Join(",", players)}");
	            
				if (playersLeft.Length > 0)
				{
					FLog.Verbose("Grid","Players left: "+string.Join(",", playersLeft));
					grid.Remove(playersLeft);
				}

				if (playersJoined.Length > 0)
				{
					FLog.Verbose("Grid","Players joined: "+string.Join(",", playersJoined));
					grid.Fit(playersJoined);
				}

				// position move requests
				foreach (var p in lobby.Players)
				{
					var destPos = p.GetProperty(FLLobbyService.KEY_POSITION_REQUEST); 
					if (string.IsNullOrEmpty(destPos)) continue;
					var newIndex = Int32.Parse(destPos);
					var currentIndex = grid.GetPosition(p.Id);
					var oldPlayer = grid.GetPlayer(newIndex);
					grid.Place(currentIndex, oldPlayer);
					grid.Place(newIndex, p.Id);
				}

				var finalGridString = grid.ToString();
				if (initialGridString != finalGridString)
				{
					await MainInstaller.ResolveServices().FLLobbyService
						.SetMatchProperty(FLLobbyService.KEY_LOBBY_MATCH_PLAYER_POSITIONS, grid.ToString());
				}
			});
		}

		public UniTask Dispose() => _gridUpdates.Dispose();

		public void RequestToGoToPosition(Lobby lobby, int position)
		{
			MainInstaller.ResolveServices().FLLobbyService
				.SetMatchPlayerProperty(FLLobbyService.KEY_POSITION_REQUEST, position.ToString()).Forget();
		}	
	}
}