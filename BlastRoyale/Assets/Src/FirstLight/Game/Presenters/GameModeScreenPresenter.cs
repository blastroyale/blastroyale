using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Configs.Utils;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Domains.HomeScreen;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.StateMachines;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.Server.SDK.Modules;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using Unity.Services.Lobbies;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter is responsible to select the game mode to start the match
	/// </summary>
	public class GameModeScreenPresenter : UIPresenterData<GameModeScreenPresenter.StateData>
	{
		private const string VISIBLE_GAMEMODE_BUTTON = "game-mode-card--element-";

		public class StateData
		{
			public string ForceViewEventDetails;
			public Action<GameModeInfo> GameModeChosen;
			public Action CustomGameChosen;
			public Action OnBackClicked;
		}

		[SerializeField] private VisualTreeAsset _buttonAsset;

		[SerializeField, Required, TabGroup("Animation")]
		private PlayableDirector _newEventDirector;

		[SerializeField] private Sprite _buyEventTicketSprite;

		private Button _closeButton;
		private ScrollView _buttonsSlider;
		private ScreenHeaderElement _header;
		private MatchSettingsButtonElement _mapButton;
		private List<GameId> _mapGameIds;

		private List<GameModeSelectionButtonView> _buttonViews;
		private IGameServices _services;

		private CancellationTokenSource _cancelSelection = null;
		private IGameDataProvider _dataProviders;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProviders = MainInstaller.ResolveData();
		}

		protected override void QueryElements()
		{
			_buttonViews = new List<GameModeSelectionButtonView>();
			_buttonsSlider = Root.Q<ScrollView>("ButtonsSlider").Required();
			_header = Root.Q<ScreenHeaderElement>("Header").Required();
			_header.backClicked = Data.OnBackClicked;
			_mapButton = Root.Q<MatchSettingsButtonElement>("MapButton").Required();

			_mapButton.clicked += OnMapButtonClicked;
			_mapButton.SetValue(_services.GameModeService.SelectedMap.GetLocalization());

			var orderNumber = 1;
			// Clear the slide from the test values
			_buttonsSlider.Clear();
			// Add game modes buttons
			foreach (var slot in _services.GameModeService.Slots)
			{
				if (slot.Entry.MatchConfig == null) continue;
				var button = _buttonAsset.Instantiate();
				button.userData = slot;
				button.AttachView(this, out GameModeSelectionButtonView view);
				view.NewEventDirector = _newEventDirector;
				slot.Entry.MatchConfig.MatchType = MatchType.Matchmaking;
				view.SetData("GameModeButton" + orderNumber, slot, GetVisibleClass(orderNumber++));
				view.Clicked += OnModeButtonClicked;
				view.ClickedInfo += OnInfoButtonClicked;
				_buttonViews.Add(view);

				view.Selected = _services.GameModeService.SelectedGameMode.Value.Equals(slot);
				_buttonsSlider.Add(button);
			}

			// Add custom game button
			var gameModeInfo = new GameModeInfo
			{
				Entry = new FixedGameModeEntry()
				{
					MatchConfig = new SimulationMatchConfig
					{
						Mutators = Mutator.None,
						MatchType = MatchType.Custom,
						TeamSize = 1
					},
					CardModifier = "custom",
					Title = LocalizableString.FromTerm(ScriptTerms.UITGameModeSelection.custom_game_title),
					Description = LocalizableString.FromTerm(ScriptTerms.UITGameModeSelection.custom_game_description)
				}
			};
			var createGameButton = _buttonAsset.Instantiate();
			createGameButton.AttachView(this, out GameModeSelectionButtonView customGameView);
			customGameView.SetData("CustomGameButton", gameModeInfo, GetVisibleClass(orderNumber), VISIBLE_GAMEMODE_BUTTON + "last");
			customGameView.Clicked += OnCustomGameClicked;
			customGameView.LevelLock(UnlockSystem.GameModesCustomGames);
			_buttonViews.Add(customGameView);
			_buttonsSlider.Add(createGameButton);
			UpdateMapDropdownVisibility();

			if (Data.ForceViewEventDetails != null)
			{
				foreach (var view in _buttonViews)
				{
					if (view.GameModeInfo.Entry is EventGameModeEntry ev &&
						Data.ForceViewEventDetails == view.GameModeInfo.Entry.MatchConfig.UniqueConfigId)
					{
						OnInfoButtonClicked(view);
						break;
					}
				}
			}
		}

		private void OnMapButtonClicked()
		{
			var validMaps = _services.GameModeService.ValidMatchmakingMaps;

			PopupPresenter.OpenSelectMap(mapId =>
			{
				var mapGid = Enum.Parse<GameId>(mapId);
				_services.GameModeService.SelectedMap = mapGid;
				_mapButton.SetValue(mapGid.GetLocalization());
				PopupPresenter.Close().Forget();
			}, validMaps, _services.GameModeService.SelectedMap.ToString(), true).Forget();
		}

		private void UpdateMapDropdownVisibility()
		{
			_mapButton.SetDisplay(_services.GameModeService.SelectedGameMode.Value.Entry.MatchConfig.MapId == GameId.Any.ToString());
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_services.GameModeService.SelectedGameMode.Observe(OnGameModeUpdated);
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyUpdated += OnLobbyChanged;
			return base.OnScreenOpen(reload);
		}

		protected override UniTask OnScreenClose()
		{
			_services.GameModeService.SelectedGameMode.StopObserving(OnGameModeUpdated);
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyUpdated -= OnLobbyChanged;
			return base.OnScreenClose();
		}

		private string GetVisibleClass(int orderNumber)
		{
			return VISIBLE_GAMEMODE_BUTTON + (orderNumber > 4 ? "large" : orderNumber);
		}

		private void OnCustomGameClicked(GameModeSelectionButtonView info)
		{
			Data.CustomGameChosen();
			// _services.UIService.OpenScreen<MatchListScreenPresenter>().Forget();
		}

		private void OnSlotUpdated(int index, GameModeInfo previous, GameModeInfo current,
								   ObservableUpdateType updateType)
		{
			_buttonViews[index].SetData(current);
		}

		private void OnLobbyChanged(ILobbyChanges changes)
		{
			foreach (var view in _buttonViews)
			{
				if (view.IsCustomGame()) continue;
				if (view.GameModeInfo.Entry == null) continue;
				view.UpdateDisabledStatus();
			}
		}

		private void OnModeButtonClicked(GameModeSelectionButtonView info)
		{
			var entry = info.GameModeInfo.Entry;
			if (entry is EventGameModeEntry ev && ev.PriceToJoin != null)
			{
				OnInfoButtonClicked(info);
				return;
			}

			SelectGameMode(info);
		}

		private void OnInfoButtonClicked(GameModeSelectionButtonView info)
		{
			var entry = info.GameModeInfo.Entry;
			if (entry is EventGameModeEntry ev && ev.PriceToJoin != null)
			{
				var alreadyHasTicket = _dataProviders.GameEventsDataProvider.HasPass(entry.MatchConfig.UniqueConfigId);
				if (!alreadyHasTicket)
				{
					var text = (ev.PriceToJoin.Value + " " + CurrencyItemViewModel.GetRichTextIcon(ev.PriceToJoin.RewardId))
						.WithFontSize("150%");

					PopupPresenter.OpenMatchInfo(info.GameModeInfo, text, ScriptLocalization.UITGameModeSelection.participate_event_label, () =>
						PopupPresenter.Close().ContinueWith(() =>
						{
							_services.GenericDialogService.OpenPurchaseOrNotEnough(new GenericPurchaseDialogPresenter.TextPurchaseData()
							{
								Price = ItemFactory.Legacy(ev.PriceToJoin),
								TextFormat = "You are about to spend {0}\non event participation",
								OnConfirm = () =>
								{
									_services.CommandService.ExecuteCommand(new BuyEventPassCommand()
										{UniqueEventId = entry.MatchConfig.UniqueConfigId});
									SelectAndStartMatchmaking(info);
								},
								OnGoToShopRequired = () =>
								{
									Data.OnBackClicked?.Invoke();
								}
							});
						})).Forget();
					return;
				}
			}

			PopupPresenter.OpenMatchInfo(info.GameModeInfo, null, null, () =>
				PopupPresenter.Close().ContinueWith(() =>
				{
					SelectAndStartMatchmaking(info);
				})).Forget();
		}

		private void SelectAndStartMatchmaking(GameModeSelectionButtonView info)
		{
			_services.GameModeService.SelectedGameMode.Value = info.GameModeInfo;
			_services.HomeScreenService.SetForceBehaviour(HomeScreenForceBehaviourType.Matchmaking);
			Data.GameModeChosen(info.GameModeInfo);
		}

		private void SelectGameMode(GameModeSelectionButtonView info)
		{
			SelectButton(info);
			_cancelSelection?.Cancel();
			_cancelSelection = new CancellationTokenSource();

			if (info.GameModeInfo.Entry is EventGameModeEntry ev && ev.IsPaid)
			{
				return;
			}

			ChangeGameModeCoroutine(info, _cancelSelection.Token).Forget();
		}

		private async UniTask ChangeGameModeCoroutine(GameModeSelectionButtonView info, CancellationToken tok)
		{
			await UniTask.WaitForSeconds(0.5f, cancellationToken: tok);
			_services.GameModeService.SelectedGameMode.Value = info.GameModeInfo;
			Data.GameModeChosen(info.GameModeInfo);
		}

		private void SelectButton(GameModeSelectionButtonView info)
		{
			foreach (var buttonView in _buttonViews)
			{
				buttonView.Selected = false;
			}

			info.Selected = true;
		}

		/// <summary>
		/// Listen for selected gamemode changes, when party size changes it selects a proper gamemode matching the team size
		/// </summary>
		private void OnGameModeUpdated(GameModeInfo _, GameModeInfo newGamemode)
		{
			foreach (var buttonView in _buttonViews)
			{
				buttonView.Selected = buttonView.GameModeInfo.Entry == newGamemode.Entry;
			}

			UpdateMapDropdownVisibility();
		}
	}
}