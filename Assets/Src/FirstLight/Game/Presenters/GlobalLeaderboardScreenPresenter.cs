using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

		private const int DefaultTrophies = 0;
		private const string UssLeaderboardEntryGlobal = "leaderboard-entry--global";
		private const string UssLeaderboardEntryPositionerHighlight = "leaderboard-entry-positioner--highlight";
		private const string UssLeaderboardPanelLocalPlayerFixed = "leaderboard-panel__local-player-fixed";
		private const string UssLeaderboardEntry = "leaderboard-entry";
		private const string UssLeaderboardButton = "leaderboard-button";
		private const string UssLeaderboardButtonIndicator = UssLeaderboardButton+"__indicator";
		private const string NoDisplayNameReplacement = "Unamed00000";

		[SerializeField] private VisualTreeAsset _leaderboardEntryAsset;

		private VisualElement _leaderboardPanel;
		private VisualElement _fixedLocalPlayerHolder;
		private ScreenHeaderElement _header;
		private VisualElement _loadingSpinner;
		private VisualElement _leaderboardOptions;
		private VisualElement _leaderboardDescription;
		
		private LocalizedLabel _pointsName;
		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private GameLeaderboard _viewingBoard;
		private Dictionary<GameLeaderboard, Button> _buttons = new();
		private ListView _leaderboardListView;
		private VisualElement _localPlayerVisualElement;
		private VisualElement _viewingIndicator;

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
			SetupButtons();
			DisplayLeaderboard(_services.LeaderboardService.Leaderboards.First());
		}

		private void SetupButtons()
		{
			foreach (var leaderboard in _services.LeaderboardService.Leaderboards)
			{
				var button = new Button();
				button.AddToClassList(UssLeaderboardButton);
				_buttons[leaderboard] = button;
				button.text = leaderboard.Name;
				button.clicked += () => DisplayLeaderboard(leaderboard);
				_leaderboardOptions.Add(button);
			}
		}

		private void DisplayLeaderboard(GameLeaderboard board)
		{
			var button = _buttons[board];
			button.Add(_viewingIndicator);
			_leaderboardListView.Clear();
			_leaderboardListView.RefreshItems();
			_leaderboardListView.SetVisibility(false);
			_loadingSpinner.SetDisplay(true);
			_fixedLocalPlayerHolder.Clear();
			_leaderboardPanel.RemoveFromClassList(UssLeaderboardPanelLocalPlayerFixed);
			_services.LeaderboardService.GetTopRankLeaderboard(
				board.MetricName,
				r => OnLeaderboardTopRanksReceived(board, r));
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
			_pointsName = root.Q<LocalizedLabel>("Trophies").Required();
			_leaderboardOptions = root.Q<VisualElement>("LeaderboardOptions").Required();
			_leaderboardDescription = root.Q<VisualElement>("LeaderboardDescription").Required();
			_leaderboardListView.DisableScrollbars();
			_leaderboardListView.SetVisibility(false);
			_viewingIndicator = new VisualElement();
			_viewingIndicator.AddToClassList(UssLeaderboardButtonIndicator);

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
			
			leaderboardEntryView.SetIcon(_viewingBoard.IconClass);
		}
		
		private void OnLeaderboardTopRanksReceived(GameLeaderboard board, GetLeaderboardResult result)
		{
			var resultPos = result.Leaderboard.Count < _services.LeaderboardService.MaxEntries
				? result.Leaderboard.Count
				: _services.LeaderboardService.MaxEntries;
			_pointsName.Localize(board.Name);
			_playfabLeaderboardEntries.Clear();
			_viewingBoard = board;
			FLog.Verbose($"Displaying Leaderboard for metric {board.MetricName}");
			for (int i = 0; i < resultPos; i++)
			{
				if (result.Leaderboard[i].PlayFabId == _dataProvider.AppDataProvider.PlayerId)
				{
					_localPlayerPos = i;
				}

				_playfabLeaderboardEntries.Add(result.Leaderboard[i]);
			}

			_leaderboardListView.Clear();
			_leaderboardListView.RefreshItems();
			_leaderboardListView.itemsSource = _playfabLeaderboardEntries;
			_leaderboardListView.bindItem = BindLeaderboardEntry;

			if (_localPlayerPos != -1)
			{
				FLog.Verbose("Found local player in leaderboard, scrolling to it");
				StartCoroutine(RepositionScrollToLocalPlayer());
				return;
			}

			FLog.Verbose("Local player not found in leaderboard, getting elements near him");
			_services.LeaderboardService.GetNeighborRankLeaderboard(board.MetricName,
				OnLeaderboardNeighborRanksReceived);
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

			view.SetIcon(_viewingBoard.IconClass);
			
			newEntry.AddToClassList(UssLeaderboardEntryGlobal);
			newEntry.AddToClassList(UssLeaderboardEntryPositionerHighlight);

			_leaderboardPanel.AddToClassList(UssLeaderboardPanelLocalPlayerFixed);

			_fixedLocalPlayerHolder.Clear();
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