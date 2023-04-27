using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for the Global Leaderboards Screen
	/// </summary>
	public class GlobalLeaderboardScreenPresenter : UiToolkitPresenterData<GlobalLeaderboardScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnBackClicked;
		}

		private const int DefaultTrophies = 1000;
		private const string UssLeaderboardEntryGlobal = "leaderboard-entry--global";
		private const string UssLeaderboardEntryPositionerHighlight = "leaderboard-entry-positioner--highlight";
		private const string UssLeaderboardPanelLocalPlayerFixed = "leaderboard-panel__local-player-fixed";
		private const string NoDisplayNameReplacement = "Unamed00000";

		[SerializeField] private VisualTreeAsset _leaderboardEntryAsset;

		private VisualElement _leaderboardPanel;
		private VisualElement _fixedLocalPlayerHolder;
		private ScreenHeaderElement _header;
		private VisualElement _loadingSpinner;

		private IGameServices _services;
		private IGameDataProvider _dataProvider;

		private ListView _leaderboardListView;
		private VisualElement _localPlayerVisualElement;

		private int _localPlayerPos = -1;

		private readonly Dictionary<VisualElement, LeaderboardEntryView> _leaderboardEntryMap = new();
		private readonly List<PlayerLeaderboardEntry> _playfabLeaderboardEntries = new();

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			_services.GameBackendService.GetTopRankLeaderboard(GameConstants.Network.LEADERBOARD_TOP_RANK_AMOUNT,
				OnLeaderboardTopRanksReceived, OnLeaderboardRequestError);
		}

		protected override void QueryElements(VisualElement root)
		{
			_header = root.Q<ScreenHeaderElement>("Header").Required();
			_header.backClicked += Data.OnBackClicked;
			_header.homeClicked += Data.OnBackClicked;
			_fixedLocalPlayerHolder = root.Q<VisualElement>("FixedLocalPlayerHolder").Required();
			_leaderboardPanel = root.Q<VisualElement>("LeaderboardPanel").Required();
			_leaderboardListView = root.Q<ListView>("LeaderboardList").Required();
			_loadingSpinner = root.Q<AnimatedImageElement>("LoadingSpinner").Required();

			
			_leaderboardListView.DisableScrollbars();
			_leaderboardListView.SetVisibility(false);

			_loadingSpinner.SetDisplay(true);	
			
			_leaderboardListView.makeItem = CreateLeaderboardEntry;
			_leaderboardListView.bindItem = BindLeaderboardEntry;
			root.SetupClicks(_services);
		}

		private VisualElement CreateLeaderboardEntry()
		{
			var newEntry = _leaderboardEntryAsset.Instantiate();
			newEntry.AttachView(this, out LeaderboardEntryView view);
			newEntry.AddToClassList(UssLeaderboardEntryGlobal);

			_leaderboardEntryMap[newEntry] = view;

			return newEntry;
		}

		private void BindLeaderboardEntry(VisualElement element, int index)
		{
			var leaderboardEntryView = _leaderboardEntryMap[element];
			var leaderboardEntry = _playfabLeaderboardEntries[index];

			var isLocalPlayer = leaderboardEntry.PlayFabId == _dataProvider.AppDataProvider.PlayerId;
			
			leaderboardEntry.DisplayName ??= NoDisplayNameReplacement;

			leaderboardEntryView.SetData(leaderboardEntry.Position + 1,
				leaderboardEntry.DisplayName[..^5], -1,
				leaderboardEntry.StatValue, isLocalPlayer, leaderboardEntry.Profile.AvatarUrl);
		}

		private void OnLeaderboardRequestError(PlayFabError error)
		{
#if UNITY_EDITOR
			OpenLeaderboardRequestErrorGenericDialog(error);
#else
			OpenOnLeaderboardRequestErrorPopup(error);
#endif
			_loadingSpinner.SetDisplay(false);
			
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

		private void OnLeaderboardTopRanksReceived(GetLeaderboardResult result)
		{			
			var resultPos = result.Leaderboard.Count < GameConstants.Network.LEADERBOARD_TOP_RANK_AMOUNT
				? result.Leaderboard.Count
				: GameConstants.Network.LEADERBOARD_TOP_RANK_AMOUNT;

			_playfabLeaderboardEntries.Clear();

			for (int i = 0; i < resultPos; i++)
			{
				if (result.Leaderboard[i].PlayFabId == _dataProvider.AppDataProvider.PlayerId)
				{
					_localPlayerPos = i;
				}

				_playfabLeaderboardEntries.Add(result.Leaderboard[i]);
			}

			_leaderboardListView.itemsSource = _playfabLeaderboardEntries;

			_leaderboardListView.bindItem = BindLeaderboardEntry;

			if (_localPlayerPos != -1)
			{
				StartCoroutine(RepositionScrollToLocalPlayer());
				return;
			}

			_services.GameBackendService.GetNeighborRankLeaderboard(GameConstants.Network.LEADERBOARD_NEIGHBOR_RANK_AMOUNT,
				OnLeaderboardNeighborRanksReceived, OnLeaderboardRequestError);
		}

		private void OnLeaderboardNeighborRanksReceived(GetLeaderboardAroundPlayerResult result)
		{
			var newEntry = _leaderboardEntryAsset.Instantiate();
			newEntry.AttachView(this, out LeaderboardEntryView view);
			var leaderboardEntry = result.Leaderboard[0];

			int trophies = leaderboardEntry.StatValue == 0 ? DefaultTrophies : leaderboardEntry.StatValue;
			
			leaderboardEntry.DisplayName ??= NoDisplayNameReplacement;

			view.SetData(leaderboardEntry.Position + 1,
				leaderboardEntry.DisplayName.Substring(0, leaderboardEntry.DisplayName.Length - 5), -1,
				trophies, true, _dataProvider.AppDataProvider.AvatarUrl);

			newEntry.AddToClassList(UssLeaderboardEntryGlobal);
			newEntry.AddToClassList(UssLeaderboardEntryPositionerHighlight);

			_leaderboardPanel.AddToClassList(UssLeaderboardPanelLocalPlayerFixed);

			_fixedLocalPlayerHolder.Add(newEntry);
			_leaderboardListView.SetVisibility(true);
			_loadingSpinner.SetDisplay(false);
		}

		IEnumerator RepositionScrollToLocalPlayer()
		{
			yield return new WaitForSeconds(1);

			float height = _leaderboardListView.layout.height;
			float elemHeight = _leaderboardListView.fixedItemHeight;
			int elementsOnScreen = (int)(height / elemHeight);

			int indexToScrollTo = (_localPlayerPos + elementsOnScreen / 2) - 1;

			if (indexToScrollTo > _playfabLeaderboardEntries.Count)
			{
				_leaderboardListView.ScrollToItem(_playfabLeaderboardEntries.Count - 1);
			}
			else
			{
				_leaderboardListView.ScrollToItem(indexToScrollTo);
			}
			_loadingSpinner.SetDisplay(false);
			_leaderboardListView.SetVisibility(true);
		}
	}
}