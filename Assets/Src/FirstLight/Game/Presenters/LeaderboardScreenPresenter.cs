using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MatchHudViews;
using FirstLight.Services;
using I2.Loc;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Loot Screen, where players can equip items and upgrade loot.
	/// </summary>
	public class LeaderboardScreenPresenter : AnimatedUiPresenterData<LeaderboardScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action BackClicked;
		}

		[SerializeField] private PlayerRankEntryView _playerRankEntryRef;
		[SerializeField] private Transform _topRankSpawnTransform;
		[SerializeField] private Transform _farRankSpawnTransform;
		[SerializeField] private GameObject _farRankLeaderboardRoot;
		[SerializeField] private TextMeshProUGUI _seasonEndText;
		[SerializeField] private Button _backButton;

		private IGameServices _services;
		private IObjectPool<PlayerRankEntryView> _playerRankPool;

		private int lowestTopRankedPosition = 0;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_backButton.onClick.AddListener(OnBackClicked);
		}

		private void OnDestroy()
		{
			_backButton.onClick.RemoveAllListeners();
		}

		protected override void OnClosed()
		{
			base.OnClosed();

			_playerRankPool.DespawnAll();
			_farRankLeaderboardRoot.SetActive(false);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			_farRankLeaderboardRoot.SetActive(false);

			var nextResetDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month + 1, 1);
			var timeDiff = nextResetDate - DateTime.UtcNow;

			// TODO - ALL TEXT ON THIS PRESENTER NEEDS TO BE ADDED, HOOKED UP AND LOCALIZED. THIS IS PLACEHOLDER
			_seasonEndText.text = "Season ends in " + timeDiff.ToString(@"d\d\ h\h\ mm\m");

			// Leaderboard is requested and displayed in 2 parts
			// First - top players, then, if needed - current player and neighbors, in a separate anchored section
			_services.PlayfabService.GetTopRankLeaderboard(GameConstants.Network.LEADERBOARD_LADDER_NAME,
			                                               GameConstants.Network.LEADERBOARD_TOP_RANK_AMOUNT,
			                                               OnLeaderboardTopRanksReceived, OnPlayfabError);
		}

		private void OnPlayfabError(PlayFabError error)
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};

			if (error.ErrorDetails != null)
			{
				FLog.Error(JsonConvert.SerializeObject(error.ErrorDetails));
			}

			_services.GenericDialogService.OpenDialog(error.ErrorMessage, false, confirmButton);
		}

		private void OnLeaderboardTopRanksReceived(GetLeaderboardResult result)
		{
			_playerRankPool =
				new GameObjectPool<PlayerRankEntryView>((uint) result.Leaderboard.Count, _playerRankEntryRef);

			bool localPlayerInTopRanks = false;

			lowestTopRankedPosition = result.Leaderboard[result.Leaderboard.Count - 1].Position;

			for (int i = result.Leaderboard.Count - 1; i >= 0; i--)
			{
				var isLocalPlayer = result.Leaderboard[i].PlayFabId == PlayFabSettings.staticPlayer.PlayFabId;
				var newEntry = _playerRankPool.Spawn();
				newEntry.transform.SetParent(_topRankSpawnTransform);
				newEntry.gameObject.SetActive(true);
				newEntry.SetInfo(result.Leaderboard[i].Position + 1, result.Leaderboard[i].DisplayName,
				                 result.Leaderboard[i].StatValue, isLocalPlayer, null);

				if (isLocalPlayer)
				{
					localPlayerInTopRanks = true;
				}
			}

			if (localPlayerInTopRanks) return;

			_services.PlayfabService.GetNeighborRankLeaderboard(GameConstants.Network.LEADERBOARD_LADDER_NAME,
			                                                    GameConstants.Network.LEADERBOARD_NEIGHBOR_RANK_AMOUNT,
			                                                    OnLeaderboardNeighborRanksReceived, OnPlayfabError);
		}

		private void OnLeaderboardNeighborRanksReceived(GetLeaderboardAroundPlayerResult result)
		{
			var localPlayer = result.Leaderboard.First(x => x.PlayFabId == PlayFabSettings.staticPlayer.PlayFabId);

			// If the rank above player joins with the top ranks leaderboard, we just append player to the normal leaderboard
			if (Math.Abs(lowestTopRankedPosition - localPlayer.Position) == 1)
			{
				var newEntry = _playerRankPool.Spawn();
				newEntry.gameObject.SetActive(true);
				newEntry.SetInfo(localPlayer.Position + 1, localPlayer.DisplayName, localPlayer.StatValue, true, null);
				return;
			}

			_farRankLeaderboardRoot.SetActive(true);

			for (int i = 0; i < result.Leaderboard.Count; i++)
			{
				var isLocalPlayer = result.Leaderboard[i].PlayFabId == PlayFabSettings.staticPlayer.PlayFabId;
				var newEntry = _playerRankPool.Spawn();
				newEntry.gameObject.SetActive(true);
				newEntry.transform.SetParent(_farRankSpawnTransform);
				newEntry.SetInfo(result.Leaderboard[i].Position + 1, result.Leaderboard[i].DisplayName,
				                 result.Leaderboard[i].StatValue, isLocalPlayer, null);
			}
		}

		private void OnBackClicked()
		{
			Data.BackClicked();
		}
	}
}