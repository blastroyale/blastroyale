using FirstLight.Game.Logic;
using I2.Loc;
using Photon.Realtime;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This class shows player status inside of PlayerListHolder objects
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

		public Player Player { get; private set; }
		public string PlayerName { get; private set; }
		public bool IsHost { get; private set; }
		public bool IsLocal { get; private set; }

		/// <summary>
		/// Set the information of this player entry based on the given strings and host status
		/// </summary>
		public void SetInfo(Player player, bool isLocal, bool isHost, bool isLoaded)
		{
			Player = player;
			
			if (player == null)
			{
				IsHost = false;
				IsLocal = false;
				PlayerName = "";
				_hostIconObject.SetActive(false);
				_playerNameText.text = "";
				_playerStatus.text = "";
				return;
			}

			IsHost = isHost;
			IsLocal = isLocal;
			PlayerName = player.NickName;

			var col = IsHost ? _hostColor : _regularColor;
			col = isLocal ? _localColor : col;

			_hostIconObject.SetActive(isHost);

			_playerNameText.text = PlayerName;

			if (!isLoaded)
			{
				_playerStatus.text = ScriptLocalization.AdventureMenu.ReadyStatusLoading;
			}
			else
			{
				_playerStatus.text = isHost ? ScriptLocalization.AdventureMenu.ReadyStatusHost
											: ScriptLocalization.AdventureMenu.ReadyStatusReady;
			}

			_playerNameText.color = col;
			_playerStatus.color = col;
		}
	}
}