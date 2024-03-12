using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter is responsible to select the game mode to start the match
	/// </summary>
	[LoadSynchronously]
	public class GameModeSelectionPresenter : UiToolkitPresenterData<GameModeSelectionPresenter.StateData>
	{
		private const string VISIBLE_GAMEMODE_BUTTON = "visible-gamemodebutton";
		
		public struct StateData
		{
			public Action<GameModeInfo> GameModeChosen;
			public Action CustomGameChosen;
			
			public Action OnHomeClicked;
			public Action OnBackClicked;
		}
		
		[SerializeField] private VisualTreeAsset _buttonAsset;
		[SerializeField] private VisualTreeAsset _comingSoonAsset;
		
		private Button _closeButton;
		private ScrollView _buttonsSlider;
		private ScreenHeaderElement _header;
		
		private List<GameModeSelectionButtonView> _buttonViews;
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services.GameModeService.Slots.Observe(OnSlotUpdated);
			_buttonViews = new List<GameModeSelectionButtonView>();
		}
		
		protected override void QueryElements(VisualElement root)
		{
			_buttonsSlider = root.Q<ScrollView>("ButtonsSlider").Required();
			_header = root.Q<ScreenHeaderElement>("Header").Required();
			_header.backClicked += Data.OnBackClicked;
			
			var orderNumber = 1;
			var canUseSquads = _gameDataProvider.HasNfts();
			
			// Add game modes buttons
			foreach (var slot in _services.GameModeService.Slots)
			{
				if(slot.Entry.NFT && !canUseSquads) continue;
				var button = _buttonAsset.Instantiate();
				button.userData = slot;
				button.AttachView(this, out GameModeSelectionButtonView view);
				view.SetData("GameModeButton"+orderNumber, GetVisibleClass(orderNumber++), slot);
				view.Clicked += OnModeButtonClicked;
				_buttonViews.Add(view);

				view.Selected = _services.GameModeService.SelectedGameMode.Value.Equals(slot);
				
				_buttonsSlider.Add(button);
			}

			// Add custom game button
			var gameModeInfo = new GameModeInfo();
			gameModeInfo.Entry.GameModeId = GameConstants.GameModeId.FAKEGAMEMODE_CUSTOMGAME;
			gameModeInfo.Entry.MatchType = MatchType.Custom;
			gameModeInfo.Entry.Mutators = new List<string>();
			var createGameButton = _buttonAsset.Instantiate();
			createGameButton.AttachView(this, out GameModeSelectionButtonView customGameView);
			customGameView.SetData("CustomGameButton", GetVisibleClass(orderNumber++), gameModeInfo);
			customGameView.Clicked += OnCustomGameClicked;
			_buttonViews.Add(customGameView);
			_buttonsSlider.Add(createGameButton);
			
			// Add Coming soon button
			var comingSoonGameButton = _comingSoonAsset.Instantiate();
			var comingSoonButtonRoot = comingSoonGameButton.Q<VisualElement>("ComingSoonGameModeButton");
			comingSoonButtonRoot.AddToClassList(GetVisibleClass(orderNumber++));
			_buttonsSlider.Add(comingSoonGameButton);
		}

		private string GetVisibleClass(int orderNumber)
		{
			return VISIBLE_GAMEMODE_BUTTON + (orderNumber > 4 ? "" : orderNumber);
		}

		private void OnCustomGameClicked(GameModeSelectionButtonView info)
		{
			Data.CustomGameChosen();
		}

		private void OnSlotUpdated(int index, GameModeInfo previous, GameModeInfo current,
								   ObservableUpdateType updateType)
		{
			_buttonViews[index].SetData(current);
		}
		
		private void OnModeButtonClicked(GameModeSelectionButtonView info)
		{
			SelectButton(info);

			StartCoroutine(ChangeGameModeCoroutine(info));
		}

		private IEnumerator ChangeGameModeCoroutine(GameModeSelectionButtonView info)
		{
			yield return new WaitForSeconds(0.3f);
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
	}
}
