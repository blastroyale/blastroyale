using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.UIElements;
using FirstLight.Server.SDK.Models;
using FirstLight.UiService;
using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles the player statistics screen.
	/// </summary>
	[LoadSynchronously]
	public class PlayerStatisticsPopupPresenter : UiToolkitPresenterData<PlayerStatisticsPopupPresenter.StateData>
	{
		public struct StateData
		{
			public string PlayerId;
			public Action OnCloseClicked;
			public Action OnEditNameClicked;
		}
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		
		private Label _nameLabel;
		private VisualElement _content;
		private VisualElement _loadingSpinner;
		private PlayerAvatarElement _pfpImage;
		
		private Label[] _statLabels;
		private Label[] _statValues;
		private VisualElement[] _statContainers;

		private int _pfpRequestHandle = -1;
		
		private const int StatisticMaxSize = 6;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void SubscribeToEvents()
		{
			base.SubscribeToEvents();
			_gameDataProvider.AppDataProvider.DisplayName.InvokeObserve(OnDisplayNameChanged);
		}

		protected override void UnsubscribeFromEvents()
		{
			base.UnsubscribeFromEvents();
			_gameDataProvider.AppDataProvider.DisplayName.StopObserving(OnDisplayNameChanged);
		}

		private void OnDisplayNameChanged(string _, string current)
		{
			_nameLabel.text = _gameDataProvider.AppDataProvider.DisplayNameTrimmed;
		}

		protected override void QueryElements(VisualElement root)
		{
			_statLabels = new Label[StatisticMaxSize];
			_statValues = new Label[StatisticMaxSize];
			_statContainers = new VisualElement[StatisticMaxSize];

			root.Q<ImageButton>("EditNameButton").clicked += () =>Data.OnEditNameClicked();
			root.Q<ImageButton>("CloseButton").clicked += Data.OnCloseClicked;
			root.Q<VisualElement>("Background").RegisterCallback<ClickEvent, StateData>((_, data) => data.OnCloseClicked(), Data);

			_pfpImage = root.Q<PlayerAvatarElement>("Avatar").Required();
			_content = root.Q<VisualElement>("Content").Required();
			_nameLabel = root.Q<Label>("NameLabel").Required();
			_loadingSpinner = root.Q<AnimatedImageElement>("LoadingSpinner").Required();

			for (int i = 0; i < StatisticMaxSize; i++)
			{
				_statContainers[i] = root.Q<VisualElement>($"StatsContainer{i}").Required();
				_statContainers[i].visible = false;
				
				_statLabels[i] = root.Q<Label>($"StatName{i}").Required();
				_statValues[i] = root.Q<Label>($"StatValue{i}").Required();
			}

			_content.visible = false;
			_loadingSpinner.visible = true;
			
			root.SetupClicks(_services);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
		
			SetupPopup();
		}

		protected override async Task OnClosed()
		{
			base.OnClosed();
			_services.RemoteTextureService.CancelRequest(_pfpRequestHandle);
		}

		private void SetStatInfo(int index, PublicPlayerProfile result, string statName, string statLoc)
		{
			var stat = result.Statistics.FirstOrDefault(s => s.Name == statName);
			_statLabels[index].text = statLoc;
			_statValues[index].text = stat.Value.ToString();
			_statContainers[index].visible = true;
			_statContainers[index].parent.RegisterCallback<MouseDownEvent>(e => OpenLeaderboard(statLoc, statName));
		}

		private void OpenLeaderboard(string name, string metric)
		{
			_services.GameUiService.CloseUi<PlayerStatisticsPopupPresenter>();
			_services.GameUiService.OpenScreen<GlobalLeaderboardScreenPresenter, GlobalLeaderboardScreenPresenter.StateData>(new ()
			{
				OnBackClicked = () => _services.MessageBrokerService.Publish(new MainMenuShouldReloadMessage()),
				ShowSpecificLeaderboard = new GameLeaderboard(name, metric)
			});
		}

		private void SetupPopup()
		{
			_content.visible = false;
			_loadingSpinner.visible = true;
			_services.ProfileService.GetPlayerPublicProfile(Data.PlayerId, (result) =>
			{
			    if (!IsOpen) return;
				
				_nameLabel.text = result.Name.Remove(result.Name.Length - 5);

				SetStatInfo(0, result, GameConstants.Stats.RANKED_GAMES_PLAYED_EVER, ScriptLocalization.MainMenu.RankedGamesPlayedEver);
				SetStatInfo(1, result, GameConstants.Stats.RANKED_GAMES_WON_EVER, ScriptLocalization.MainMenu.RankedGamesWon);
				SetStatInfo(2, result, GameConstants.Stats.RANKED_KILLS_EVER, ScriptLocalization.MainMenu.RankedKills);
				SetStatInfo(3, result, GameConstants.Stats.GAMES_PLAYED_EVER, ScriptLocalization.MainMenu.GamesPlayedEver);
				SetStatInfo(4, result, GameConstants.Stats.GAMES_WON_EVER, ScriptLocalization.MainMenu.GamesWonEver);
				SetStatInfo(5, result, GameConstants.Stats.KILLS_EVER, ScriptLocalization.MainMenu.KillsEver);
				
				_pfpImage.SetAvatar(result.AvatarUrl);
				_pfpImage.SetLevel(_gameDataProvider.PlayerDataProvider.Level.Value);
				_pfpImage.RegisterCallback<MouseDownEvent>(e => OpenLeaderboard(ScriptLocalization.General.Level, GameConstants.Stats.FAME));
				_content.visible = true;
				_loadingSpinner.visible = false;
			});
		}
	}
}