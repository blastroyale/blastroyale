using System;
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

		[SerializeField] private PlayerRankEntryView _playerRankEntryPlaceholder;
		[SerializeField] private GameObject _playerGapEntryPlaceholder;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private IObjectPool<PlayerRankEntryView> _playerRankPool;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}
		
		protected override void OnOpened()
		{
			base.OnOpened();
			
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			var leaderboardRequest = new GetLeaderboardRequest()
			{
				AuthenticationContext = PlayFabSettings.staticPlayer,
				StartPosition = 0,
				MaxResultsCount = 50,
				StatisticName = GameConstants.Network.LEADERBOARD_LADDER_NAME
				
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
			foreach (var entry in result.Leaderboard)
			{
				Debug.LogError(entry.DisplayName + " " + entry.StatValue);
			}
		}
		
		private void OnLeaderboardNeighborRanksReceived(GetLeaderboardResult result)
		{
			foreach (var entry in result.Leaderboard)
			{
				Debug.LogError(entry.DisplayName + " " + entry.StatValue);
			}
		}
	}
}