using System;
using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class GameModeSelectionPresenter : UiToolkitPresenterData<GameModeSelectionPresenter.StateData>
	{
		[SerializeField] private VisualTreeAsset _buttonAsset;

		private Button _closeButton;
		private ScrollView _buttonsSlider;
		private List<GameModeSelectionButtonView> _buttonViews;
		
		public struct StateData
		{
			public Action GameModeChosen;
			public Action CustomGameChosen;
			public Action LeaveGameModeSelection;
		}
		
		private IGameServices _services;

		void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_services.GameModeService.Slots.Observe(OnSlotUpdated);

			_buttonViews = new List<GameModeSelectionButtonView>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_buttonsSlider = root.Q<ScrollView>("ButtonsSlider").Required();
			_closeButton = root.Q<Button>("CloseButton").Required();

			_closeButton.clicked += OnCloseButtonClicked;
			
			// Add game modes buttons
			foreach (var slot in _services.GameModeService.Slots)
			{
				var button = _buttonAsset.Instantiate();
				button.AttachView(this, out GameModeSelectionButtonView view);
				view.SetData(slot);
				view.Clicked += OnModeButtonClicked;
				_buttonViews.Add(view);

				view.Selected = _services.GameModeService.SelectedGameMode.Value.Equals(slot);
				
				_buttonsSlider.Add(button);
			}

			// Add custom game button
			var gameModeInfo = new GameModeInfo();
			gameModeInfo.Entry.GameModeId = "Custom Game";
			var createGameButton = _buttonAsset.Instantiate();
			createGameButton.AttachView(this, out GameModeSelectionButtonView customGameView);
			customGameView.SetData(gameModeInfo);
			customGameView.Clicked += OnCustomGameClicked;
			_buttonViews.Add(customGameView);
			_buttonsSlider.Add(createGameButton);
		}

		private void OnCloseButtonClicked()
		{
			Data.LeaveGameModeSelection();
		}

		private void OnCustomGameClicked(GameModeSelectionButtonView info)
		{
			SelectButton(info);

			Data.CustomGameChosen();
		}

		private void OnSlotUpdated(int index, GameModeInfo previous, GameModeInfo current,
								   ObservableUpdateType updateType)
		{
			Assert.AreEqual(ObservableUpdateType.Updated, updateType);

			_buttonViews[index].SetData(current);
		}
		
		private void OnModeButtonClicked(GameModeSelectionButtonView info)
		{
			SelectButton(info);
			
			_services.GameModeService.SelectedGameMode.Value = info.GameModeInfo;
			Data.GameModeChosen();
		}

		private void SelectButton(GameModeSelectionButtonView info)
		{
			if (info.Selected)
			{
				return;
			}

			foreach (var buttonView in _buttonViews)
			{
				buttonView.Selected = false;
			}
			
			info.Selected = true;
		}
	}
}
