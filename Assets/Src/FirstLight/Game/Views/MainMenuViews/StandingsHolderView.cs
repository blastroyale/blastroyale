using System.Collections.Generic;
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
	public class StandingsHolderView : MonoBehaviour
	{
		[SerializeField] private PlayerResultEntryView _resultEntryViewRef;
		[SerializeField] private GameObject _xpHolder;
		[SerializeField] private GameObject _coinsHolder;
		[SerializeField] private RectTransform _contentTransform;
		[SerializeField] private int _verticalEntrySpacing = 14;
		[SerializeField] private UnityEngine.UI.Button _blockerButton;

		private IObjectPool<PlayerResultEntryView> _playerResultPool;
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private bool _showExtra;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			var playersLimit = _services.NetworkService.QuantumClient.CurrentRoom.MaxPlayers;
			
			_playerResultPool = new GameObjectPool<PlayerResultEntryView>(playersLimit, _resultEntryViewRef);

			for (var i = 0; i < playersLimit; i++)
			{
				_playerResultPool.Spawn();
			}

			if (playersLimit < 10)
			{
				var entryHeight = ((RectTransform) _resultEntryViewRef.transform).sizeDelta.y;
				_contentTransform.sizeDelta = new Vector2(_contentTransform.sizeDelta.x,
				                                          (entryHeight + _verticalEntrySpacing) *
				                                          (playersLimit + 1));
			}

			_blockerButton.onClick.AddListener(OnCloseClicked);
			_resultEntryViewRef.gameObject.SetActive(false);
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer,
			                                                  onlyIfActiveAndEnabled: true);
		}

		/// <summary>
		/// Initialises the Standings Holder with current player ranks, kills and deaths.
		/// If _showExtra is set to true, also shows XP and coins earned.
		/// </summary>
		public void Initialise(List<QuantumPlayerMatchData> playerData, bool showExtra = true,
		                       bool enableBlockerButton = true)
		{
			_coinsHolder.SetActive(showExtra);
			_xpHolder.SetActive(showExtra);
			_blockerButton.gameObject.SetActive(enableBlockerButton);
			_showExtra = showExtra;

			Setup(playerData);
		}

		private void Setup(List<QuantumPlayerMatchData> playerData)
		{
			var pool = _playerResultPool.SpawnedReadOnly;
			playerData.SortByPlayerRank();
			playerData.Reverse();

			// Do the descending order. From the highest to the lowest value
			for (var i = 0; i < pool.Count; i++)
			{
				pool[i].SetInfo(playerData[i], _showExtra);
			}
		}

		private void OnCloseClicked()
		{
			gameObject.SetActive(false);
		}

		/// <summary>
		/// The scoreboard could update whilst it's open, e.g. players killed whilst looking at it, etc.
		/// </summary>
		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			Setup(new List<QuantumPlayerMatchData>(callback.PlayersMatchData));
		}
	}
}