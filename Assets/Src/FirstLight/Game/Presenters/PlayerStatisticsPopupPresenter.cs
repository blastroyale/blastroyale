using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.UIElements;
using FirstLight.Modules.UIService.Runtime;
using FirstLight.Server.SDK.Models;
using FirstLight.UiService;
using FirstLight.UIService;
using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles the player statistics screen.
	/// </summary>
	[UILayer(UIService2.UILayer.Popup)]
	public class PlayerStatisticsPopupPresenter : UIPresenterData2<PlayerStatisticsPopupPresenter.StateData>
	{
		public class StateData
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
		
		private const int StatisticMaxSize = 4;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		private void OnDisplayNameChanged(string _, string current)
		{
			_nameLabel.text = _gameDataProvider.AppDataProvider.DisplayNameTrimmed;
		}

		protected override void QueryElements()
		{
			_statLabels = new Label[StatisticMaxSize];
			_statValues = new Label[StatisticMaxSize];
			_statContainers = new VisualElement[StatisticMaxSize];

			Root.Q<ImageButton>("EditNameButton").clicked += () => Data.OnEditNameClicked();
			Root.Q<ImageButton>("CloseButton").clicked += Data.OnCloseClicked;
			Root.Q<VisualElement>("Background").RegisterCallback<ClickEvent, StateData>((_, data) => data.OnCloseClicked(), Data);

			_pfpImage = Root.Q<PlayerAvatarElement>("Avatar").Required();
			_content = Root.Q<VisualElement>("Content").Required();
			_nameLabel = Root.Q<Label>("NameLabel").Required();
			_loadingSpinner = Root.Q<AnimatedImageElement>("LoadingSpinner").Required();

			for (int i = 0; i < StatisticMaxSize; i++)
			{
				_statContainers[i] = Root.Q<VisualElement>($"StatsContainer{i}").Required();
				_statContainers[i].visible = false;

				_statLabels[i] = Root.Q<Label>($"StatName{i}").Required();
				_statValues[i] = Root.Q<Label>($"StatValue{i}").Required();
			}
			
			// Hiding 2 more stats slots. Will be used later
			Root.Q<VisualElement>($"StatsWidget4").Required().SetDisplay(false);
			Root.Q<VisualElement>($"StatsWidget5").Required().SetDisplay(false);

			_content.visible = false;
			_loadingSpinner.visible = true;

			Root.SetupClicks(_services);
		}


		protected override UniTask OnScreenOpen(bool reload)
		{
			_gameDataProvider.AppDataProvider.DisplayName.InvokeObserve(OnDisplayNameChanged);
			SetupPopup();
			return base.OnScreenOpen(reload);
		}

		protected override UniTask OnScreenClose()
		{
			_gameDataProvider.AppDataProvider.DisplayName.StopObserving(OnDisplayNameChanged);
			_services.RemoteTextureService.CancelRequest(_pfpRequestHandle);
			return base.OnScreenClose();
		}

		private void SetStatInfo(int index, PublicPlayerProfile result, string statName, string statLoc)
		{
			var stat = result.Statistics.FirstOrDefault(s => s.Name == statName);
			_statLabels[index].text = statLoc;
			_statValues[index].text = stat.Value.ToString();
			_statContainers[index].visible = true;
			_statContainers[index].parent.RegisterCallback<MouseDownEvent>(e => OpenLeaderboard(statLoc, statName));
		}

		private void OpenLeaderboard(string leaderboardName, string metric)
		{
			_services.UIService.CloseScreen<PlayerStatisticsPopupPresenter>();
			_services.UIService.OpenScreen<GlobalLeaderboardScreenPresenter>(new GlobalLeaderboardScreenPresenter.StateData()
			{
				OnBackClicked = () => _services.MessageBrokerService.Publish(new MainMenuShouldReloadMessage()),
				ShowSpecificLeaderboard = new GameLeaderboard(leaderboardName, metric)
			}).Forget();
		}

		private void SetupPopup()
		{
			_content.visible = false;
			_loadingSpinner.visible = true;
			_services.ProfileService.GetPlayerPublicProfile(Data.PlayerId, (result) =>
			{
				// TODO: Race condition if you close and quickly reopen the popup
				if (!_services.UIService.IsScreenOpen<PlayerStatisticsPopupPresenter>()) return;

				_nameLabel.text = result.Name.Remove(result.Name.Length - 5);

				SetStatInfo(0, result, GameConstants.Stats.RANKED_GAMES_PLAYED_EVER, ScriptLocalization.MainMenu.RankedGamesPlayedEver);
				SetStatInfo(1, result, GameConstants.Stats.RANKED_GAMES_WON_EVER, ScriptLocalization.MainMenu.RankedGamesWon);
				SetStatInfo(2, result, GameConstants.Stats.RANKED_KILLS_EVER, ScriptLocalization.MainMenu.RankedKills);
				SetStatInfo(3, result, GameConstants.Stats.RANKED_DEATHS_EVER, ScriptLocalization.MainMenu.RankedDeaths);
				
				_pfpImage.SetAvatar(result.AvatarUrl);
				_pfpImage.SetLevel(_gameDataProvider.PlayerDataProvider.Level.Value);
				_pfpImage.RegisterCallback<MouseDownEvent>(e => OpenLeaderboard(ScriptLocalization.General.Level, GameConstants.Stats.FAME));
				_content.visible = true;
				_loadingSpinner.visible = false;
			});
		}
	}
}
