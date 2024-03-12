using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq; 
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.UiService;
using JetBrains.Annotations;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UIElements;

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
			[CanBeNull] public GameLeaderboard ShowSpecificLeaderboard;
		}

		private const int DefaultTrophies = 0;
		private const string UssLeaderboardEntryGlobal = "leaderboard-entry--global";
		private const string UssLeaderboardEntryPositionerHighlight = "leaderboard-entry-positioner--highlight";
		private const string UssLeaderboardPanelLocalPlayerFixed = "leaderboard-panel__local-player-fixed";
		private const string UssLeaderboardEntry = "leaderboard-entry";
		private const string UssLeaderboardButton = "leaderboard-button";
		private const string UssLeaderboardButtonHighlight = UssLeaderboardButton+"--highlight";
		private const string UssLeaderboardButtonIndicator = UssLeaderboardButton+"__indicator";
		private const string NoDisplayNameReplacement = "Unamed00000";

		[SerializeField] private VisualTreeAsset _leaderboardEntryAsset;

		private VisualElement _leaderboardPanel;
		private VisualElement _fixedLocalPlayerHolder;
		private ScreenHeaderElement _header;
		private VisualElement _loadingSpinner;
		private VisualElement _leaderboardOptions;
		private VisualElement _descriptionContainer;
		private Label _leaderboardDescription;
		private Label _leaderboardTitle;
		private Button _discordButton;
		private Button _infoButton;
		private Label _rewardsText;
		private Label _endsIn;
		private VisualElement _endsInContainer;
		private VisualElement _headerIcon;
		private VisualElement _rewardsWidget;
		private Label _rewardsTitle;
		private LocalizedLabel _pointsName;
		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private GameLeaderboard _viewingBoard;
		private int _viewingSeason;
		private Dictionary<GameLeaderboard, Button> _buttons = new();
		private ListView _leaderboardListView;
		private VisualElement _localPlayerVisualElement;
		private VisualElement _viewingIndicator;

		private int _localPlayerPos = -1;

		private readonly Dictionary<VisualElement, LeaderboardEntryView> _leaderboardEntryMap = new();
		private readonly List<PlayerLeaderboardEntry> _playfabLeaderboardEntries = new();

		protected override void QueryElements(VisualElement root)
		{
			_header = root.Q<ScreenHeaderElement>("Header").Required();
			_header.backClicked += Data.OnBackClicked;
			_fixedLocalPlayerHolder = root.Q<VisualElement>("FixedLocalPlayerHolder").Required();
			_leaderboardPanel = root.Q<VisualElement>("LeaderboardPanel").Required();
			_leaderboardListView = root.Q<ListView>("LeaderboardList").Required();
			_loadingSpinner = root.Q<AnimatedImageElement>("LoadingSpinner").Required();
			_pointsName = root.Q<LocalizedLabel>("Trophies").Required();
			_descriptionContainer = root.Q("LeaderboardDescription").Required();
			_leaderboardOptions = root.Q<VisualElement>("LeaderboardOptions").Required();
			_leaderboardDescription = root.Q<Label>("DescText").Required();
			_leaderboardTitle = root.Q<Label>("LeaderboardTitle").Required();
			_headerIcon = root.Q("LeaderboardIcon").Required();
			_endsIn = root.Q<Label>("EndsInText").Required();
			_endsInContainer = root.Q<VisualElement>("EndsInContainer").Required();
			_discordButton = root.Q<Button>("DiscordButton").Required();
			_rewardsText = root.Q<Label>("RewardsText").Required();
			_rewardsWidget = root.Q("RewardsWidget").Required();
			_rewardsTitle = root.Q<Label>("LeaderboardTitleDesc").Required();
			_infoButton = root.Q<Button>("InfoButton").Required();
			_headerIcon.SetVisibility(false);
			_leaderboardListView.DisableScrollbars();
			_leaderboardListView.SetVisibility(false);
			_viewingIndicator = new VisualElement();
			_viewingIndicator.AddToClassList(UssLeaderboardButtonIndicator);

			_loadingSpinner.SetDisplay(true);

			_infoButton.clicked += () =>
			{
				// TODO: Language not working for some reason
				_endsInContainer.OpenTooltip(Root, "Progress will be reset at the end of the season", TooltipDirection.TopRight,
				                             TooltipPosition.BottomLeft, 20, 20);
			};
			_discordButton.clicked += () => Application.OpenURL(GameConstants.Links.DISCORD_SERVER);
			_leaderboardListView.makeItem = CreateLeaderboardEntry;
			_leaderboardListView.bindItem = BindLeaderboardEntry;
			root.SetupClicks(_services);
		}
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			if (Data.ShowSpecificLeaderboard != null)
			{
				DisplayLeaderboard(Data.ShowSpecificLeaderboard);
				_descriptionContainer.SetVisibility(false);
			}
			else
			{
				SetupButtons();
				DisplayLeaderboard(_services.LeaderboardService.Leaderboards.First());
			}
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
			foreach(var b in _buttons.Values) b.RemoveFromClassList(UssLeaderboardButtonHighlight);
			if (_buttons.TryGetValue(board, out var button))
			{
				button.Add(_viewingIndicator);
				button.AddToClassList(UssLeaderboardButtonHighlight);
			}
			_localPlayerPos = -1;
			_leaderboardListView.Clear();
			_leaderboardListView.RefreshItems();
			_leaderboardListView.SetVisibility(false);
			_descriptionContainer.SetVisibility(false);
			_headerIcon.SetVisibility(false);
			_loadingSpinner.SetDisplay(true);
			_fixedLocalPlayerHolder.Clear();
			_leaderboardPanel.RemoveFromClassList(UssLeaderboardPanelLocalPlayerFixed);
			_services.LeaderboardService.GetTopRankLeaderboard(
				board.MetricName,
				r => OnLeaderboardTopRanksReceived(board, r));
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

			var borderColor = _services.LeaderboardService.GetRankColor(_viewingBoard, leaderboardEntry.Position + 1);
			leaderboardEntryView.SetData(leaderboardEntry.Position + 1,
				leaderboardEntry.DisplayName[..^5], -1,
				leaderboardEntry.StatValue, isLocalPlayer, leaderboardEntry.Profile.AvatarUrl, leaderboardEntry.PlayFabId, borderColor);
			
			leaderboardEntryView.SetIcon(GetIconClass());
		}

		private SeasonConfig GetViewingSeasonConfig()
		{
			return _services.LeaderboardService.GetConfigs().GetConfig(_viewingBoard).GetSeason(_viewingSeason);;
		}

		private bool HasSeasonConfig()
		{
			return _services.LeaderboardService.GetConfigs().ContainsKey(_viewingBoard.MetricName);
		}
		
		/// <summary>
		/// Fills the right side of the screen (LeaderboardDescription)
		/// Has seasonal information read from a mix of playfab and configs
		/// </summary>
		private void DisplaySeasonData(GameLeaderboard board, GetLeaderboardResult result)
		{
			if (!HasSeasonConfig())
			{
				_descriptionContainer.SetVisibility(false);
				return;
			}
			DateTime endTime = DateTime.UtcNow;
			var seasonConfig = GetViewingSeasonConfig();
			var hasRewards = !string.IsNullOrEmpty(seasonConfig.Rewards); // TODO: Read from playfab prize tables
			
			_leaderboardDescription.text = seasonConfig.Desc;
			_leaderboardTitle.text = board.Name;
			
			if (hasRewards)
			{
				_rewardsText.text = seasonConfig.Rewards;
				_rewardsTitle.text = seasonConfig.RewardsTitle;
			}
			
			_rewardsWidget.SetDisplay(hasRewards);
			_rewardsTitle.SetVisibility(hasRewards);
			
			_descriptionContainer.SetVisibility(true);
			if (board == _services.LeaderboardService.Leaderboards.First())
			{
				_headerIcon.SetVisibility(true);
			}
			
			if (!string.IsNullOrEmpty(seasonConfig.ManualEndTime))
			{
				endTime = DateTime.ParseExact(seasonConfig.ManualEndTime, "dd/MM/yyyy", CultureInfo.InvariantCulture);
			}
			else if (!result.NextReset.HasValue)
			{
				FLog.Warn($"Missing leaderboard {board.Name} end time");
				_endsIn.text = $"Not Scheduled";
				_endsInContainer.SetDisplay(false);
				_endsInContainer.SetVisibility(false);
				return;
			}
			else
			{
				endTime = result.NextReset.Value;
			}
			
			_endsInContainer.SetDisplay(true);
			_endsInContainer.SetVisibility(true);
			var daysTillReset = (int)Math.Ceiling((endTime - DateTime.UtcNow).TotalDays);
			_endsIn.text = $"Ends in {daysTillReset} days";
		}
		
		private void OnLeaderboardTopRanksReceived(GameLeaderboard board, GetLeaderboardResult result)
		{
			var resultPos = result.Leaderboard.Count < _services.LeaderboardService.MaxEntries
				? result.Leaderboard.Count
				: _services.LeaderboardService.MaxEntries;
			_pointsName.Localize(board.Name);
			_playfabLeaderboardEntries.Clear();
			_viewingBoard = board;
			_viewingSeason = result.Version;
			DisplaySeasonData(board, result);
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
				trophies, true, _dataProvider.AppDataProvider.AvatarUrl, leaderboardEntry.PlayFabId, Color.white);
			
			view.SetIcon(GetIconClass());
			
			newEntry.AddToClassList(UssLeaderboardEntryGlobal);
			newEntry.AddToClassList(UssLeaderboardEntryPositionerHighlight);
	
			_leaderboardPanel.AddToClassList(UssLeaderboardPanelLocalPlayerFixed);

			_fixedLocalPlayerHolder.Clear();
			_fixedLocalPlayerHolder.Add(newEntry);
			_leaderboardListView.SetVisibility(true);
			_loadingSpinner.SetDisplay(false);
		}
		
		public string GetIconClass()
		{
			if (HasSeasonConfig())
			{
				return GetViewingSeasonConfig().Icon;
			}
			return null;
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