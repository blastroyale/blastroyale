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
		private string[] _playerIds;
		public int GetPosition(string player) => Array.IndexOf(_playerIds, player);
		public string GetPlayer(int index) => _playerIds[index];
		public HashSet<string> PresentPlayers => _playerIds.ToHashSet();
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
				Place(GetEmptySlot(), p);
			}
		}
		
		public void Place(int position, string player)
		{
			_playerIds[position] = player;
			FLog.Verbose("Grid",$"{player} placed in slot {position}");
		}

		public void Remove(params string[] players)
		{
			var toRemove = players.ToHashSet();
			foreach (var kp in _playerIds.Select((p, i) => (p, i)).Where(kp => !string.IsNullOrEmpty(kp.p) && toRemove.Contains(kp.p)))
			{
				_playerIds[kp.i] = "";
				FLog.Verbose("Grid",$"Slot {kp.i} cleared");
			}
		}
	}
}