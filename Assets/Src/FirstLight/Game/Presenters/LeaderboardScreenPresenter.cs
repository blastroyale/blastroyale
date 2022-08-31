using System;
using System.Collections.Generic;
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
		[SerializeField] private Button _backButton;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private IObjectPool<PlayerRankEntryView> _playerRankPool;
		
		private int lowestTopRank = 0;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_backButton.onClick.AddListener(OnBackClicked);
		}

		private void OnDestroy()
		{
			_backButton.onClick.RemoveAllListeners();
			
			Debug.LogError("ON ABSOLUTE DESTRUCTION!!!!!!!");
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			_farRankLeaderboardRoot.SetActive(false);
			
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			// Leaderboard is requested and displayed in 2 parts
			// First - top players, then, if needed - current player and neighbors, in a separate anchored section
			var leaderboardRequest = new GetLeaderboardRequest()
			{
				AuthenticationContext = PlayFabSettings.staticPlayer,
				StatisticName = GameConstants.Network.LEADERBOARD_LADDER_NAME,
				StartPosition = 0,
				MaxResultsCount = GameConstants.Network.LEADERBOARD_TOP_RANK_AMOUNT
			};
			
			PlayFabClientAPI.GetLeaderboard(leaderboardRequest, OnLeaderboardTopRanksReceived, OnPlayfabError);
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
			_playerRankPool = new GameObjectPool<PlayerRankEntryView>((uint)result.Leaderboard.Count, _playerRankEntryRef);

			bool localPlayerInTopRanks = false;

			lowestTopRank = result.Leaderboard[result.Leaderboard.Count - 1].Position;
			
			for (int i = result.Leaderboard.Count - 1; i >= 0; i--)
			{
				var newEntry = _playerRankPool.Spawn();
				newEntry.gameObject.SetActive(true);
				newEntry.SetInfo( result.Leaderboard[i].Position+1, result.Leaderboard[i].DisplayName,result.Leaderboard[i].StatValue,null);
				
				if (result.Leaderboard[i].PlayFabId == PlayFabSettings.staticPlayer.PlayFabId)
				{
					localPlayerInTopRanks = true;
				}
			}

			if (localPlayerInTopRanks) return;
			
			var neighborLeaderboardRequest = new GetLeaderboardAroundPlayerRequest()
			{
				AuthenticationContext = PlayFabSettings.staticPlayer,
				StatisticName = GameConstants.Network.LEADERBOARD_LADDER_NAME,
				MaxResultsCount = GameConstants.Network.LEADERBOARD_PLAYER_RANK_NEIGHBOR_RANGE
			};
			
			PlayFabClientAPI.GetLeaderboardAroundPlayer(neighborLeaderboardRequest, OnLeaderboardNeighborRanksReceived, OnPlayfabError);
		}
		
		private void OnLeaderboardNeighborRanksReceived(GetLeaderboardAroundPlayerResult result)
		{
			_farRankLeaderboardRoot.SetActive(true);

			var localPlayerIndex = GameConstants.Network.LEADERBOARD_PLAYER_RANK_NEIGHBOR_RANGE;
			var playerAboveLocalPlayerIndex = localPlayerIndex + 1;
			
			// If the rank above player joins with the top ranks leaderboard, we just append player to the normal leaderboard
			if (result.Leaderboard[playerAboveLocalPlayerIndex].Position == lowestTopRank)
			{
				var newEntry = _playerRankPool.Spawn();
				newEntry.gameObject.SetActive(true);
				newEntry.SetInfo( result.Leaderboard[localPlayerIndex].Position+1, result.Leaderboard[localPlayerIndex].DisplayName,result.Leaderboard[localPlayerIndex].StatValue,null);
				return;
			}

			for (int i = result.Leaderboard.Count - 1; i >= 0; i--)
			{
				var newEntry = _playerRankPool.Spawn();
				newEntry.gameObject.SetActive(true);
				newEntry.transform.SetParent(_farRankSpawnTransform);
				newEntry.SetInfo( result.Leaderboard[i].Position+1, result.Leaderboard[i].DisplayName,result.Leaderboard[i].StatValue,null);
			}
		}

		private void OnBackClicked()
		{
			Data.BackClicked();
		}
	}
}