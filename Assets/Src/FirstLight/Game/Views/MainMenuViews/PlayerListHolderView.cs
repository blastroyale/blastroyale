using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MatchHudViews;
using FirstLight.Services;
using Photon.Realtime;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Used to display the current standings of all players in the game. Who is in the lead, where the current player is, etc.
	/// </summary>
	public class PlayerListHolderView : MonoBehaviour
	{
		[SerializeField] private PlayerNameEntryView _nameEntryViewRef;
		[SerializeField] private RectTransform _contentTransform;

		private IObjectPool<PlayerNameEntryView> _playerNamePool;
		
		private bool _showExtra;
		private List<PlayerNameEntryView> _activePlayerEntries = new List<PlayerNameEntryView>();
		private bool _finalPreload = false;
		private Action<Player> _kickPlayerCallback;
		
		/// <summary>
		/// Initialises the player list with <paramref name="playerLimit"/> amount of player slots
		/// </summary>
		public void Init(uint playerLimit, Action<Player> kickPlayerCallback)
		{
			_finalPreload = false;
			_kickPlayerCallback = kickPlayerCallback;

			if (_playerNamePool != null && _playerNamePool.SpawnedReadOnly.Count > 0)
			{
				_playerNamePool.DespawnAll();
				_activePlayerEntries.Clear();
			}

			_playerNamePool = new GameObjectPool<PlayerNameEntryView>(playerLimit, _nameEntryViewRef);

			for (var i = 0; i < playerLimit; i++)
			{
				var newEntry = _playerNamePool.Spawn();
				_activePlayerEntries.Add(newEntry);
				newEntry.SetInfo(null, false, false, false, string.Empty, _kickPlayerCallback);
			}

			_nameEntryViewRef.gameObject.SetActive(false);
		}

		/// <summary>
		/// Forces a refresh of all players, with the new _finalPreload phase value set
		/// </summary>
		/// <param name="finalPreload"></param>
		public void SetFinalPreloadPhase(bool finalPreload)
		{
			_finalPreload = finalPreload;

			foreach (var playerNameEntryView in _activePlayerEntries)
			{
				if (playerNameEntryView.Player != null)
				{
					AddOrUpdatePlayer(playerNameEntryView.Player, false);
				}
			}
		}

		/// <summary>
		/// Adds a player to the list, or updates them if already there
		/// </summary>
		public void AddOrUpdatePlayer(Player player, bool sortList = true)
		{ 
			var existingEntry = _activePlayerEntries.FirstOrDefault(x => x.Player == player);
			var isLoaded = _finalPreload
				               ? (bool) player.CustomProperties[GameConstants.Network.PLAYER_PROPS_ALL_LOADED]
				               : (bool) player.CustomProperties[GameConstants.Network.PLAYER_PROPS_CORE_LOADED];

			if (existingEntry != null)
			{
				existingEntry.SetInfo(player, player.IsLocal, player.IsMasterClient, isLoaded, player.GetTeamId(), _kickPlayerCallback);
			}
			else
			{
				PlayerNameEntryView emptyEntry = GetNextEmptyPlayerEntrySlot();

				if (emptyEntry != null)
				{
					emptyEntry.SetInfo(player, player.IsLocal, player.IsMasterClient, isLoaded, player.GetTeamId(), _kickPlayerCallback);
				}
			}

			if (sortList)
			{
				SortPlayerList();
			}
		}

		/// <summary>
		/// Removes player from the list view based on the provided name
		/// </summary>
		public void RemovePlayer(Player player)
		{
			var existingEntry = _activePlayerEntries.FirstOrDefault(x => x.Player == player);

			if (existingEntry != null)
			{
				existingEntry.SetInfo(null, false, false, false, string.Empty, null);

				SortPlayerList();
			}
		}

		/// <summary>
		/// Requests to check if the list holder has Player in it, currently instantiated
		/// </summary>
		public bool Has(Player player)
		{
			if (_playerNamePool == null || _playerNamePool.SpawnedReadOnly.Count == 0)
			{
				return false;
			}

			foreach (var playerEntry in _playerNamePool.SpawnedReadOnly)
			{
				if (playerEntry.Player == player)
				{
					return true;
				}
			}

			return false;
		}

		private void SortPlayerList()
		{
			_activePlayerEntries.Sort((a, b) =>
			{
				var rank = a.IsLocal.CompareTo(b.IsLocal) + a.IsHost.CompareTo(b.IsHost);

				if (rank == 0)
				{
					rank = a.PlayerName.Length.CompareTo(b.PlayerName.Length);
				}

				return rank;
			});

			_activePlayerEntries.Reverse();

			for (int i = 0; i < _activePlayerEntries.Count - 1; i++)
			{
				_activePlayerEntries[i].transform.SetSiblingIndex(i);
			}
		}

		private PlayerNameEntryView GetNextEmptyPlayerEntrySlot()
		{
			return _activePlayerEntries.FirstOrDefault(x => string.IsNullOrEmpty(x.PlayerName));
		}
	}
}