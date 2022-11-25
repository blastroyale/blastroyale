using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.UiService;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	[LoadSynchronously]
	public class GameModeSelectionPresenter : UiToolkitPresenterData<GameModeSelectionPresenter.StateData>
	{
		private const string VISIBLE_GAMEMODE_BUTTON = "visible-gamemodebutton";
		
		public struct StateData
		{
			public Action GameModeChosen;
			public Action CustomGameChosen;
			
			public Action OnHomeClicked;
			public Action OnBackClicked;
		}
		
		[SerializeField] private VisualTreeAsset _buttonAsset;
		[SerializeField] private VisualTreeAsset _comingSoonAsset;
		[SerializeField, Required] private Animation _animationScrollingBackground;
		private Button _closeButton;
		private ScrollView _buttonsSlider;
		private ScreenHeaderElement _header;
		
		private List<GameModeSelectionButtonView> _buttonViews;
		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_services.GameModeService.Slots.Observe(OnSlotUpdated);
			_buttonViews = new List<GameModeSelectionButtonView>();
		}
		
		protected override void OnOpened()
		{
			base.OnOpened();
			_animationScrollingBackground.Rewind();
			_animationScrollingBackground.Play();
		}

		protected override void QueryElements(VisualElement root)
		{
			_buttonsSlider = root.Q<ScrollView>("ButtonsSlider").Required();
			_header = root.Q<ScreenHeaderElement>("Header").Required();
			_header.homeClicked += Data.OnHomeClicked;
			_header.backClicked += Data.OnBackClicked;
			
			var orderNumber = 1;
			
			// Add game modes buttons
			foreach (var slot in _services.GameModeService.Slots)
			{
				var button = _buttonAsset.Instantiate();
				button.AttachView(this, out GameModeSelectionButtonView view);
				view.SetData(GetVisibleClass(orderNumber++), slot);
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
			customGameView.SetData(GetVisibleClass(orderNumber++), gameModeInfo);
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
			Data.GameModeChosen();
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
