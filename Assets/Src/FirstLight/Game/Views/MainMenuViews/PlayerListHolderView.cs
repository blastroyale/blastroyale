using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MatchHudViews;
using FirstLight.Services;
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
		private List<PlayerNameEntryView> _activePlayerEntries;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			var mapInfo = _gameDataProvider.AppDataProvider.SelectedMap.Value; _playerNamePool =
				new GameObjectPool<PlayerNameEntryView>((uint) mapInfo.PlayersLimit, _nameEntryViewRef);

			for (var i = 0; i < mapInfo.PlayersLimit; i++)
			{
				_activePlayerEntries.Add(_playerNamePool.Spawn());
			}
			
			_nameEntryViewRef.gameObject.SetActive(false);
		}

		/// <summary>
		/// Adds a player to the list, or updates them if already there
		/// </summary>
		public void AddOrUpdatePlayer(string playerName, string status, bool isHost = false)
		{
			var existingEntry = _activePlayerEntries.FirstOrDefault(x => x.PlayerName == playerName);

			if (existingEntry != null)
			{
				existingEntry.SetInfo(playerName,status,isHost);
			}
			else
			{
				GetNextEmptyPlayerEntrySlot().SetInfo(playerName,status,isHost);
			}
		}

		/// <summary>
		/// Removes player from the list view based on the provided name
		/// </summary>
		public void RemovePlayer(string playerName)
		{
			var existingEntry = _activePlayerEntries.FirstOrDefault(x => x.PlayerName == playerName);
			
			if (existingEntry != null)
			{
				existingEntry.SetInfo("","",false);
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