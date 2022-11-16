using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MatchHudViews;
using FirstLight.Services;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Used to display the current standings of all players in the game. Who is in the lead, where the current player is, etc.
	/// </summary>
	public class StandingsHolderView : MonoBehaviour
	{
		[SerializeField, Required] private PlayerResultEntryView _resultEntryViewRef;

		[FormerlySerializedAs("_xpHolder")] [SerializeField, Required]
		private GameObject _extraInfo;

		[SerializeField, Required] private RectTransform _contentTransform;
		[SerializeField] private int _verticalEntrySpacing = 14;
		[SerializeField, Required] private Button _blockerButton;

		private readonly List<PlayerResultEntryView> _playerResultPool = new();
		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_blockerButton.onClick.AddListener(OnCloseClicked);
			_resultEntryViewRef.gameObject.SetActive(false);
			
			QuantumEvent.Subscribe<EventOnAllPlayersJoined>(this, OnAllPlayerJoined);
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer,
			                                                  onlyIfActiveAndEnabled: true);
		}

		private void OnDestroy()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		/// <summary>
		/// Initialises the Standings Holder with current player ranks, kills and deaths.
		/// If _showExtra is set to true, also shows XP and coins earned.
		/// </summary>
		public void Initialise(int playerCount, bool showExtra = false, bool enableBlockerButton = false)
		{
			_extraInfo.SetActive(showExtra);
			_blockerButton.gameObject.SetActive(enableBlockerButton);
			_services = MainInstaller.Resolve<IGameServices>();

			UpdateBoardRows(playerCount);
		}

		/// <summary>
		/// Updates the standings order and view based on the given <paramref name="playerData"/>
		/// </summary>
		public void UpdateStandings(List<QuantumPlayerMatchData> playerData)
		{
			playerData.SortByPlayerRank(false);
			
			if (!_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				var localPlayer = QuantumRunner.Default.Game.GetLocalPlayers()[0];

				for (var i = 0; i < playerData.Count; i++)
				{
					var isLocalPlayer = localPlayer == playerData[i].Data.Player;
					_playerResultPool[i].SetInfo(playerData[i], _extraInfo.activeSelf, isLocalPlayer);
				}

				return;
			}

			for (var i = 0; i < playerData.Count; i++)
			{
				_playerResultPool[i].SetInfo(playerData[i], _extraInfo.activeSelf, false);
			}
		}

		private void UpdateBoardRows(int playerCount)
		{
			// Add missing entries
			for (var i = _playerResultPool.Count; i < playerCount; i++)
			{
				var entry = GameObjectPool<PlayerResultEntryView>.Instantiator(_resultEntryViewRef);

				entry.gameObject.SetActive(true);

				_playerResultPool.Add(entry);
			}

			// Remove extra entries
			for (var j = _playerResultPool.Count - 1; j >= playerCount; j--)
			{
				_playerResultPool.RemoveAt(j);
			}

			if (playerCount < 10)
			{
				var entryHeight = ((RectTransform) _resultEntryViewRef.transform).sizeDelta.y;
				_contentTransform.sizeDelta = new Vector2(_contentTransform.sizeDelta.x,
				                                          (entryHeight + _verticalEntrySpacing) *
				                                          (playerCount + 1));
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
			UpdateBoardRows(callback.PlayersMatchData.Count);
			UpdateStandings(callback.PlayersMatchData);
		}
		
		private void OnAllPlayerJoined(EventOnAllPlayersJoined callback)
		{
			UpdateBoardRows(callback.PlayersMatchData.Count);
			UpdateStandings(callback.PlayersMatchData);
		}
	}
}