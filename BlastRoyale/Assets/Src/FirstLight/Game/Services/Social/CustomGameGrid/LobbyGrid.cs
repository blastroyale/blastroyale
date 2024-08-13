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
		private readonly AsyncBufferedQueue _gridUpdates = new (TimeSpan.FromSeconds(0.3f), true);

		public async UniTaskVoid HandleLobbyUpdates(Lobby lobby, ILobbyChanges changes)
		{
			// only grid updates
			if (!changes.Data.Changed || !changes.Data.Value.ContainsKey(FLLobbyService.KEY_LOBBY_MATCH_PLAYER_POSITIONS)) return;
			
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
				await MainInstaller.ResolveServices().FLLobbyService.SetMatchPlayerProperty(FLLobbyService.KEY_POSITION_REQUEST, "");
			}
		}
		
		public void EnqueueGridSync(Lobby lobby)
		{
			if (lobby == null || !lobby.IsLocalPlayerHost()) return;
			
			_gridUpdates.Add(async () =>
			{
				var players = lobby.RealPlayers().Select(p => p.Id).ToHashSet();
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

		public void RequestToGoToPosition(Lobby lobby, int position)
		{
			_gridUpdates.Add(async () =>
			{
				await MainInstaller.ResolveServices().FLLobbyService
					.SetMatchPlayerProperty(FLLobbyService.KEY_POSITION_REQUEST, position.ToString());
			});
		}	
	}
}