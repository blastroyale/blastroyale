using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MatchHudViews;
using FirstLight.Services;
using Photon.Realtime;
using Quantum;
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
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private bool _showExtra;
		private List<PlayerNameEntryView> _activePlayerEntries = new List<PlayerNameEntryView>();

		public void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			var mapInfo = _services.NetworkService.CurrentRoomMapConfig.Value;

			_activePlayerEntries = new List<PlayerNameEntryView>();
			_playerNamePool = new GameObjectPool<PlayerNameEntryView>((uint) mapInfo.PlayersLimit, _nameEntryViewRef);

			for (var i = 0; i < mapInfo.PlayersLimit; i++)
			{
				var newEntry = _playerNamePool.Spawn();
				_activePlayerEntries.Add(newEntry);
				newEntry.SetInfo("", "", false, false);
			}
			
			_nameEntryViewRef.gameObject.SetActive(false);
		}

		/// <summary>
		/// Adds a player to the list, or updates them if already there
		/// </summary>
		public void WipeAllSlots()
		{
			foreach (var player in _activePlayerEntries)
			{
				player.SetInfo("","", false, false);
			}
		}

		/// <summary>
		/// Adds a player to the list, or updates them if already there
		/// </summary>
		public void AddOrUpdatePlayer(string playerName, string status, bool isLocal, bool isHost)
		{
			var existingEntry = _activePlayerEntries.FirstOrDefault(x => x.PlayerName == playerName);
			
			if (existingEntry != null)
			{
				existingEntry.SetInfo(playerName,status,isLocal,isHost);
			}
			else
			{
				GetNextEmptyPlayerEntrySlot().SetInfo(playerName,status,isLocal,isHost);
			}
			
			SortPlayerList();
		}

		/// <summary>
		/// Removes player from the list view based on the provided name
		/// </summary>
		public void RemovePlayer(Player player)
		{
			var existingEntry = _activePlayerEntries.FirstOrDefault(x => x.PlayerName == player.NickName);
			
			if (existingEntry != null)
			{
				existingEntry.SetInfo("","",false,false);
				
				SortPlayerList();
			}
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

		private void OnCloseClicked()
		{
			gameObject.SetActive(false);
		}
	}
}