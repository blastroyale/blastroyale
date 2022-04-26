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
		[SerializeField] private Color _regularColor;
		[SerializeField] private Color _localColor;
		[SerializeField] private Color _hostColor;
		[SerializeField] private GameObject _hostIconObject;
		
		private IGameDataProvider _dataProvider;
		
		public string PlayerName { get; private set; }
		public bool IsHost { get; private set; }
		public bool IsLocal { get; private set; }
		
		/// <summary>
		/// Set the information of this player entry based on the given strings and host status
		/// </summary>
		public void SetInfo(string playerName, string status, bool isLocal, bool isHost)
		{
			IsHost = isHost;
			IsLocal = isLocal;
			PlayerName = playerName;
			
			var col = IsHost ? _hostColor : _regularColor;
			col = isLocal ? _localColor : col;

			_hostIconObject.SetActive(isHost);

			_playerNameText.text = playerName;
			_playerStatus.text = status;
			
			_playerNameText.color = col;
			_playerStatus.color = col;
		}
	}
}