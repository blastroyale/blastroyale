using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Quantum;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This class shows match data information for all players at the end of the match, e.g. total kills, deaths, ranking, etc.
	/// </summary>
	public class PlayerNameEntryView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _playerNameText;
		[SerializeField] private TextMeshProUGUI _playerStatus;
		
		private IGameDataProvider _dataProvider;
		
		public string PlayerName { get; private set; }
		public bool IsHost { get; private set; }

		/// <summary>
		/// Set the information of this player entry based on the given strings and host status
		/// </summary>
		public void SetInfo(string playerName, string status, bool isHost = false)
		{
			IsHost = isHost;
			PlayerName = playerName;
			
			var col = IsHost ? Color.yellow : Color.white;

			_playerNameText.text = playerName;
			_playerStatus.text = status;
			
			_playerNameText.color = col;
			_playerStatus.color = col;
		}
	}
}