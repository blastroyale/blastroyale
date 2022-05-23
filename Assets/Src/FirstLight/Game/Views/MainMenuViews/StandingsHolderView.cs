using System.Collections.Generic;
using FirstLight.Game.Views.MatchHudViews;
using FirstLight.Services;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Used to display the current standings of all players in the game. Who is in the lead, where the current player is, etc.
	/// </summary>
	public class StandingsHolderView : MonoBehaviour
	{
		[SerializeField, Required] private PlayerResultEntryView _resultEntryViewRef;
		[FormerlySerializedAs("_xpHolder")] [SerializeField, Required] private GameObject _extraInfo;
		[SerializeField, Required] private RectTransform _contentTransform;
		[SerializeField] private int _verticalEntrySpacing = 14;
		[SerializeField, Required] private UnityEngine.UI.Button _blockerButton;
		
		private readonly List<PlayerResultEntryView> _playerResultPool = new ();

		private void Awake()
		{
			_blockerButton.onClick.AddListener(OnCloseClicked);
			_resultEntryViewRef.gameObject.SetActive(false);
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer,
			                                                  onlyIfActiveAndEnabled: true);
		}

		/// <summary>
		/// Initialises the Standings Holder with current player ranks, kills and deaths.
		/// If _showExtra is set to true, also shows XP and coins earned.
		/// </summary>
		public void Initialise(int playerCount, bool showExtra, bool enableBlockerButton)
		{
			_extraInfo.SetActive(showExtra);
			_blockerButton.gameObject.SetActive(enableBlockerButton);
			
			for (var i = 0; i < playerCount; i++)
			{
				var entry = GameObjectPool<PlayerResultEntryView>.Instantiator(_resultEntryViewRef);
				
				entry.gameObject.SetActive(true);
				
				_playerResultPool.Add(entry);
			}

			if (playerCount < 10)
			{
				var entryHeight = ((RectTransform) _resultEntryViewRef.transform).sizeDelta.y;
				_contentTransform.sizeDelta = new Vector2(_contentTransform.sizeDelta.x,
				                                          (entryHeight + _verticalEntrySpacing) *
				                                          (playerCount + 1));
			}
		}

		/// <summary>
		/// Updates the standings order and view based on the given <paramref name="playerData"/>
		/// </summary>
		public void UpdateStandings(List<QuantumPlayerMatchData> playerData)
		{
			playerData.SortByPlayerRank(false);

			// Do the descending order. From the highest to the lowest value
			for (var i = 0; i < _playerResultPool.Count; i++)
			{
				_playerResultPool[i].SetInfo(playerData[i], _extraInfo.activeSelf);
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
			UpdateStandings(callback.PlayersMatchData);
		}
	}
}