﻿using System;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using I2.Loc;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This class shows player status inside of PlayerListHolder objects
	/// </summary>
	public class PlayerNameEntryView : MonoBehaviour
	{
		[SerializeField] private Button _kickButton;
		[SerializeField] private TextMeshProUGUI _playerNameText;
		[SerializeField] private TextMeshProUGUI _playerStatus;
		[SerializeField] private Color _regularColor;
		[SerializeField] private Color _localColor;
		[SerializeField] private Color _hostColor;
		[SerializeField] private GameObject _hostIconObject;

		private Action<Player> _kickPlayerClicked;
		private IGameDataProvider _dataProvider;

		public Player Player { get; private set; }
		public string PlayerName { get; private set; }
		public bool IsHost { get; private set; }
		public bool IsLocal { get; private set; }

		private void Awake()
		{
			_kickButton.onClick.AddListener(KickButtonClicked);
		}

		private void OnDestroy()
		{
			_kickButton.onClick.RemoveAllListeners();
		}
		
		/// <summary>
		/// Set the information of this player entry based on the given strings and host status
		/// </summary>
		public void SetInfo(Player player, bool isLocal, bool isHost, bool isLoaded, string partyId, Action<Player> kickPlayerClickedCallback, Color color)
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

			Color col = default;
			if (color != default)
			{
				col = color;
			}
			else
			{
				col = IsHost ? _hostColor : _regularColor;
				col = isLocal ? _localColor : col;
			}

			_hostIconObject.SetActive(isHost);

			_playerNameText.text = string.IsNullOrEmpty(partyId) ? PlayerName : $"{PlayerName} [{partyId.Replace(GameConstants.Network.MANUAL_TEAM_ID_PREFIX, "")}]";

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
			_kickPlayerClicked = kickPlayerClickedCallback;
		}

		private void KickButtonClicked()
		{
			if (Player != null)
			{
				_kickPlayerClicked?.Invoke(Player);
			}
		}
	}
}