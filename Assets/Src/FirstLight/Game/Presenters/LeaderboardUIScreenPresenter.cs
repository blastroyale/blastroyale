using System;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.NativeUi;
using FirstLight.UiService;
using I2.Loc;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
    /// <summary>
    /// Presenter for the Leaderboards and Rewards Screen
    /// </summary>
    public class LeaderboardUIScreenPresenter : UiToolkitPresenterData<LeaderboardUIScreenPresenter.StateData>
    {
        public struct StateData
        {
            public Action OnBackClicked;
        }

        private const int MAX_POS_TOP_LEADERBOARD = 100;
        [SerializeField] private VisualTreeAsset _leaderboardUIEntryAsset;
        
        private LeaderboardUIEntryView _playerRankEntryRef;
        private VisualElement _leaderboardPanel;
        private ScreenHeaderElement _header;

        private IGameServices _services;
        private IGameDataProvider _dataProvider;

        private ScrollView _leaderboardScrollView;

        private void Awake()
        {
            _services = MainInstaller.Resolve<IGameServices>();
            _dataProvider = MainInstaller.Resolve<IGameDataProvider>();
        }

        protected override void OnOpened()
        {
            base.OnOpened();

            _services.PlayfabService.GetTopRankLeaderboard(GameConstants.Network.LEADERBOARD_TOP_RANK_AMOUNT,
                OnLeaderboardTopRanksReceived, OnLeaderboardRequestError);
        }

        protected override void QueryElements(VisualElement root)
        {
            _header = root.Q<ScreenHeaderElement>("Header").Required();
            _header.backClicked += Data.OnBackClicked;
            _header.homeClicked += Data.OnBackClicked;
            _leaderboardScrollView = root.Q<ScrollView>("LeaderboardScrollView").Required();
            root.SetupClicks(_services);
        }

        private void OnLeaderboardRequestError(PlayFabError error)
        {
#if UNITY_EDITOR
            OpenLeaderboardRequestErrorGenericDialog(error);
#else
			OpenOnLeaderboardRequestErrorPopup()
#endif
            Data.OnBackClicked();
        }

        private void OpenLeaderboardRequestErrorGenericDialog(PlayFabError error)
        {
            var confirmButton = new GenericDialogButton
            {
                ButtonText = ScriptLocalization.General.OK,
                ButtonOnClick = _services.GenericDialogService.CloseDialog
            };
            if (error.ErrorDetails != null)
            {
                FLog.Error(JsonConvert.SerializeObject($"Error Message: {error.ErrorMessage}; Error Details: {error.ErrorDetails}"));
            }

            _services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, error.ErrorMessage,
                false,
                confirmButton);
        }

        private void OpenOnLeaderboardRequestErrorPopup(PlayFabError error)
        {
            var button = new AlertButton
            {
                Style = AlertButtonStyle.Positive,
                Text = ScriptLocalization.General.Confirm
            };

            NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.LeaderboardOpenError,
                error.ErrorMessage, button);
        }

        private void OnLeaderboardTopRanksReceived(GetLeaderboardResult result)
        {
            bool localPlayerInTopRanks = false;

            for (int i = 0; i < MAX_POS_TOP_LEADERBOARD + 1; i++)
            {
                var isLocalPlayer = result.Leaderboard[i].PlayFabId == _dataProvider.AppDataProvider.PlayerId;

                var newEntry = _leaderboardUIEntryAsset.Instantiate();
                newEntry.AttachView(this, out LeaderboardUIEntryView view);
                view.SetData(result.Leaderboard[i], isLocalPlayer, i + 1);
                _leaderboardScrollView.Add(newEntry);

                if (isLocalPlayer)
                {
                    localPlayerInTopRanks = true;
                }
            }

            if (localPlayerInTopRanks) return;

            _services.PlayfabService.GetNeighborRankLeaderboard(GameConstants.Network.LEADERBOARD_NEIGHBOR_RANK_AMOUNT,
                OnLeaderboardNeighborRanksReceived, OnLeaderboardRequestError);
        }

        private void OnLeaderboardNeighborRanksReceived(GetLeaderboardAroundPlayerResult result)
        {
            var localPlayer = result.Leaderboard.First(x => x.PlayFabId == PlayFabSettings.staticPlayer.PlayFabId);

            var newEntry = _leaderboardUIEntryAsset.Instantiate();
            newEntry.AttachView(this, out LeaderboardUIEntryView view);
            view.SetData(localPlayer, true, MAX_POS_TOP_LEADERBOARD + 1);
            _leaderboardScrollView.Add(newEntry);
        }
    }
}