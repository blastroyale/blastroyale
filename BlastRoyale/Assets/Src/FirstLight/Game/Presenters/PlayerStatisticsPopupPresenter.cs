using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.Config;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Data.DataTypes.Helpers;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Server.SDK.Models;
using FirstLight.UIService;
using FirstLightServerSDK.Modules;
using I2.Loc;
using PlayFab;
using Quantum;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles the player statistics screen.
	/// </summary>
	[UILayer(UILayer.Popup)]
	public class PlayerStatisticsPopupPresenter : UIPresenterData<PlayerStatisticsPopupPresenter.StateData>
	{
		public class StateData
		{
			public string BotName;
			public string PlayfabID;
			public string UnityID;
			public Action OnEditNameClicked;
			public bool CanOpenLeaderboards;
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

		private const int StatisticMaxSize = 8;

		private static List<GameId> _botAvatars = new ()
		{
			GameId.Avatar1,
			GameId.Avatar2,
			GameId.Avatar3,
			GameId.Avatar4,
			GameId.Avatar5,
			GameId.AvatarAssasinmask,
			GameId.AvatarBurger,
			GameId.AvatarBrandfemale,
			GameId.AvatarBrandmale,
		};

		private bool IsLocalPlayer => Data.PlayfabID == PlayFabSettings.staticPlayer.PlayFabId;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		/// <summary>
		/// Opens the screen and loads the player statistics for the given Unity ID
		/// </summary>
		public static async UniTask Open(string unityID)
		{
			var services = MainInstaller.ResolveServices();

			await services.UIService.OpenScreen<PlayerStatisticsPopupPresenter>(new StateData
			{
				UnityID = unityID,
			});
		}

		/// <summary>
		/// Opens the screen and loads the player statistics for the given Unity ID
		/// </summary>
		public static async UniTask OpenBot(string botName)
		{
			var services = MainInstaller.ResolveServices();

			await services.UIService.OpenScreen<PlayerStatisticsPopupPresenter>(new StateData
			{
				BotName = botName,
			});
		}

		protected override void QueryElements()
		{
			_statLabels = new Label[StatisticMaxSize];
			_statValues = new Label[StatisticMaxSize];
			_statContainers = new VisualElement[StatisticMaxSize];

			var editNameButton = Root.Q<ImageButton>("EditNameButton");
			if (IsLocalPlayer)
			{
				editNameButton.clicked += () => Data.OnEditNameClicked();
			}
			else
			{
				editNameButton.SetVisibility(false);
			}

			Root.Q<ImageButton>("CloseButton").clicked += Close;
			Root.Q<VisualElement>("Background").RegisterCallback<ClickEvent, PlayerStatisticsPopupPresenter>((_, p) => p.Close(), this);

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

			_content.visible = false;
			_loadingSpinner.visible = true;

			Root.SetupClicks(_services);
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_nameLabel.text = AuthenticationService.Instance.GetPlayerName();
			SetupPopup().Forget();
			return base.OnScreenOpen(reload);
		}

		private void Close()
		{
			_services.UIService.CloseScreen<PlayerStatisticsPopupPresenter>().Forget();
		}

		protected override UniTask OnScreenClose()
		{
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
			if (!Data.CanOpenLeaderboards) return;
			_services.UIService.CloseScreen<PlayerStatisticsPopupPresenter>();
			_services.UIService.OpenScreen<GlobalLeaderboardScreenPresenter>(new GlobalLeaderboardScreenPresenter.StateData()
			{
				OnBackClicked = () => _services.MessageBrokerService.Publish(new MainMenuShouldReloadMessage()),
				ShowSpecificLeaderboard = new GameLeaderboard(leaderboardName, metric)
			}).Forget();
		}

		private string GetAvatarUrlForBot(string botName)
		{
			var rng = new Random(botName.GetDeterministicHashCode());
			var id = _botAvatars[rng.Next(_botAvatars.Count)];
			return AvatarHelpers.GetAvatarUrl(ItemFactory.Collection(id), _services.ConfigsProvider.GetConfig<AvatarCollectableConfig>());
		}

		private PublicPlayerProfile GetProfileForBot(string botName)
		{
			var rng = new Random(botName.GetDeterministicHashCode());
			return new PublicPlayerProfile()
			{
				Name = botName,
				AvatarUrl = GetAvatarUrlForBot(botName),
				Statistics = new List<Statistic>()
				{
					new () {Name = GameConstants.Stats.RANKED_GAMES_PLAYED_EVER, Value = rng.Next(3, 8)},
					new () {Name = GameConstants.Stats.RANKED_GAMES_WON_EVER, Value = rng.Next(0, 3)},
					new () {Name = GameConstants.Stats.RANKED_KILLS_EVER, Value = rng.Next(0, 15)},
					new () {Name = GameConstants.Stats.RANKED_DAMAGE_DONE_EVER, Value = rng.Next(0, 500)},
					new () {Name = GameConstants.Stats.RANKED_AIRDROP_OPENED_EVER, Value = rng.Next(0, 3)},
					new () {Name = GameConstants.Stats.RANKED_SUPPLY_CRATES_OPENED_EVER, Value = rng.Next(1, 8)},
					new () {Name = GameConstants.Stats.RANKED_GUNS_COLLECTED_EVER, Value = rng.Next(5, 10)},
					new () {Name = GameConstants.Stats.RANKED_PICKUPS_COLLECTED_EVER, Value = rng.Next(3, 15)},
					new () {Name = GameConstants.Stats.FAME, Value = rng.Next(1, 6)},
				}
			};
		}

		private async UniTaskVoid SetupPopup()
		{
			_content.visible = false;
			_loadingSpinner.visible = true;

			PublicPlayerProfile result;
			if (Data.UnityID != null)
			{
				// If PlayfabID is null we fetch it from CloudSave.
				Data.PlayfabID ??= (await CloudSaveService.Instance.LoadPlayerDataAsync(Data.UnityID)).PlayfabID;
			}

			if (Data.PlayfabID != null)
			{
				if (!_services.UIService.IsScreenOpen<PlayerStatisticsPopupPresenter>()) return;
				result = await _services.ProfileService.GetPlayerPublicProfile(Data.PlayfabID);
				FLog.Info("Downloading profile for " + Data.PlayfabID);
			}
			else
			{
				await UniTask.Delay(UnityEngine.Random.Range(100, 300));
				result = GetProfileForBot(Data.BotName);
			}

			// TODO: Race condition if you close and quickly reopen the popup
			if (!_services.UIService.IsScreenOpen<PlayerStatisticsPopupPresenter>()) return;

			// TODO mihak: Temporary
			if (IsLocalPlayer)
			{
				_nameLabel.text = AuthenticationService.Instance.GetPlayerName();
			}
			else
			{
				_nameLabel.text = Data.UnityID == null ? result.Name : result.Name.Remove(result.Name.Length - 5);
			}

			SetStatInfo(0, result, GameConstants.Stats.RANKED_GAMES_PLAYED_EVER, ScriptLocalization.MainMenu.RankedGamesPlayedEver);
			SetStatInfo(1, result, GameConstants.Stats.RANKED_GAMES_WON_EVER, ScriptLocalization.MainMenu.RankedGamesWon);
			SetStatInfo(2, result, GameConstants.Stats.RANKED_KILLS_EVER, ScriptLocalization.MainMenu.RankedKills);
			SetStatInfo(3, result, GameConstants.Stats.RANKED_DAMAGE_DONE_EVER, ScriptLocalization.MainMenu.RankedDamageDone);
			SetStatInfo(4, result, GameConstants.Stats.RANKED_AIRDROP_OPENED_EVER, ScriptLocalization.MainMenu.RankedAirdropOpened);
			SetStatInfo(5, result, GameConstants.Stats.RANKED_SUPPLY_CRATES_OPENED_EVER, ScriptLocalization.MainMenu.RankedSupplyCratesOpened);
			SetStatInfo(6, result, GameConstants.Stats.RANKED_GUNS_COLLECTED_EVER, ScriptLocalization.MainMenu.RankedGunsCollected);
			SetStatInfo(7, result, GameConstants.Stats.RANKED_PICKUPS_COLLECTED_EVER, ScriptLocalization.MainMenu.RankedPickupsCollected);

			_pfpImage.SetAvatar(result.AvatarUrl);
			if (IsLocalPlayer)
			{
				_pfpImage.SetLevel(_gameDataProvider.PlayerDataProvider.Level.Value);
			}
			else
			{
				var stat = result.Statistics.FirstOrDefault(s => s.Name == GameConstants.Stats.FAME);
				_pfpImage.SetLevel((uint) stat.Value);
			}

			_pfpImage.RegisterCallback<MouseDownEvent>(e => OpenLeaderboard(ScriptLocalization.General.Level, GameConstants.Stats.FAME));
			_content.visible = true;
			_loadingSpinner.visible = false;
		}
	}
}