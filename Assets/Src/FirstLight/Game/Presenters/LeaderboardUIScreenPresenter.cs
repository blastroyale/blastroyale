using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
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

        [SerializeField] private VisualTreeAsset _leaderboardUIEntryAsset;

        private LeaderboardUIEntryView _playerRankEntryRef;
        private VisualElement _leaderboardPanel;
        private VisualElement _fixedLocalPlayerHolder;
        private ScreenHeaderElement _header;

        private IGameServices _services;
        private IGameDataProvider _dataProvider;

        private ScrollView _leaderboardScrollView;
        private int _localPlayerPos = -1;

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
            _fixedLocalPlayerHolder = root.Q<VisualElement>("FixedLocalPlayerHolder").Required();
            _leaderboardPanel = root.Q<VisualElement>("LeaderboardPanel").Required();

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
                FLog.Error(JsonConvert.SerializeObject(
                    $"Error Message: {error.ErrorMessage}; Error Details: {error.ErrorDetails}"));
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

        private TemplateContainer AddEntry(PlayerLeaderboardEntry playerLeaderboardEntry, bool isLocalPlayer, int pos)
        {
            var newEntry = _leaderboardUIEntryAsset.Instantiate();
            newEntry.AttachView(this, out LeaderboardUIEntryView view);
            view.SetData(playerLeaderboardEntry, isLocalPlayer, pos);
            _leaderboardScrollView.Add(newEntry);
            return newEntry;
        }

        private void OnLeaderboardTopRanksReceived(GetLeaderboardResult result)
        {
            var isLocalPlayer = false;
            var resultPos = result.Leaderboard.Count < GameConstants.Network.LEADERBOARD_TOP_RANK_AMOUNT
                ? result.Leaderboard.Count
                : GameConstants.Network.LEADERBOARD_TOP_RANK_AMOUNT;

            for (int i = 0; i < resultPos; i++)
            {
                if (result.Leaderboard[i].PlayFabId == _dataProvider.AppDataProvider.PlayerId)
                {
                    isLocalPlayer = true;
                    _localPlayerPos = i;
                }
                else
                    isLocalPlayer = false;

                AddEntry(result.Leaderboard[i], isLocalPlayer, i);
            }

            if (_localPlayerPos != -1)
            {
                StartCoroutine(RepositionScrollToLocalPlayer());
                return;
            }

            _services.PlayfabService.GetNeighborRankLeaderboard(GameConstants.Network.LEADERBOARD_NEIGHBOR_RANK_AMOUNT,
                OnLeaderboardNeighborRanksReceived, OnLeaderboardRequestError);
        }

        IEnumerator RepositionScrollToLocalPlayer()
        {
            yield return new WaitForEndOfFrame();
            _leaderboardScrollView.ScrollTo(
                _leaderboardScrollView.contentContainer.hierarchy.ElementAt(_localPlayerPos));
            _leaderboardScrollView.scrollOffset = new Vector2(_leaderboardScrollView.scrollOffset.x,
                _leaderboardScrollView.scrollOffset.y + _leaderboardScrollView.contentViewport.layout.height / 2);
        }

        private void OnLeaderboardNeighborRanksReceived(GetLeaderboardAroundPlayerResult result)
        {
            var newEntry = _leaderboardUIEntryAsset.Instantiate();
            newEntry.AttachView(this, out LeaderboardUIEntryView view);
            view.SetData(result.Leaderboard[0], true);
            _leaderboardPanel.AddToClassList("leaderboard-panel--localPlayerFixed");
            _fixedLocalPlayerHolder.Add(newEntry);
        }
    }
}