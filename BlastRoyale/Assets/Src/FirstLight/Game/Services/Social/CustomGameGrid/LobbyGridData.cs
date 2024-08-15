using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using UnityEngine.PlayerLoop;

namespace FirstLight.Game.Services.Social
{
	/// <summary>
	/// Represents the grid the players stay. Like players "seats"
	/// </summary>
	public class LobbyGridData
	{
		private readonly string[] _playerIds;
		public int GetPosition(string player) => Array.IndexOf(_playerIds, player);
		public string GetPlayer(int index) => _playerIds[index];
		public HashSet<string> PresentPlayers => _playerIds.ToHashSet(); // TODO: We should not create a new hashset every time!
		public override string ToString() => string.Join(",", _playerIds);
		public int GetEmptySlot() => Array.IndexOf(_playerIds, "");
		public IReadOnlyList<string> PositionArray => _playerIds;

		public LobbyGridData(string gridString)
		{
			_playerIds = gridString.Split(',');
		}

		public void Fit(params string[] players)
		{
			foreach (var p in players)
			{
				var emptySlot = GetEmptySlot();
				if (emptySlot >= 0)
				{
					Place(GetEmptySlot(), p);
				}
				else
				{
					FLog.Verbose("No room in grid for player "+p);
				}
			}
		}

		public void ShuffleStack()
		{
			var playersSet = PresentPlayers;
			playersSet.Remove(string.Empty);
			
			var players = playersSet.ToList();
			
			var rng = new Random();
			var n = players.Count;
			while (n > 1)
			{
				n--;
				var k = rng.Next(n + 1);
				(players[k], players[n]) = (players[n], players[k]);
			}

			for (int i = 0; i < _playerIds.Length; i++)
			{
				if (i < players.Count)
				{
					_playerIds[i] = players[i];
				}
				else
				{
					_playerIds[i] = string.Empty;
				}
			}
		}

		public void Place(int position, string player)
		{
			FLog.Verbose("Grid", $"{player} placed in slot {position}");
			_playerIds[position] = player;
		}

		public void Remove(params string[] players)
		{
			var toRemove = players.ToHashSet();
			foreach (var kp in _playerIds.Select((p, i) => (p, i)).Where(kp => !string.IsNullOrEmpty(kp.p) && toRemove.Contains(kp.p)))
			{
				_playerIds[kp.i] = "";
				FLog.Verbose("Grid", $"Slot {kp.i} cleared");
			}
		}
	}
}