using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.GridViews;
using FirstLight.Game.Views.MainMenuViews;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This View handles the Change Player Skin Menu.
	/// </summary>
	public class CratesScreenPresenter : AnimatedUiPresenterData<CratesScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnCloseClicked;
			public Action<UniqueId> LootBoxOpenClicked;
		}

		[SerializeField, Required] private Button _backButton;
		[SerializeField, Required] private GenericGridView _gridView;
		[SerializeField, Required] private TextMeshProUGUI _itemsText;
		[SerializeField, Required] private TextMeshProUGUI _descriptionText;
		[SerializeField, Required] private TextMeshProUGUI _itemTitleText;
		[SerializeField, Required] private TextMeshProUGUI _crateTierText;
		[SerializeField, Required] private TextMeshProUGUI _hardCurrencyAmountText;
		[SerializeField, Required] private Image _avatarImage;
		[SerializeField, Required] private Image _hardCurrencyImage;
		[SerializeField, Required] private Button _unlockButton;
		[SerializeField, Required] private TextMeshProUGUI _unlockButtonText;
		[SerializeField, Required] private TextMeshProUGUI _possibleRewardsText;
		[SerializeField, Required] private GameObject _crateContentsHolder;
		[SerializeField, Required] private GameObject _unlockingHolder;
		[SerializeField, Required] private GameObject _hardCurrencyHolder;
		[SerializeField, Required] private PossibleRarityCardView [] _possibleRarityCards;

		private IGameDataProvider _gameDataProvider;
		private int _selectedSlot;
		private LootBoxInventoryInfo _lootBoxInventoryInfo;  
		private List<UniqueId> _showNotifications;
		private Coroutine _coroutineTime;
		

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			_backButton.onClick.AddListener(OnBackButtonPressed);
			_unlockButton.onClick.AddListener(OnUnlockPressed);
			_showNotifications = new List<UniqueId>();
		}

		private void OnDestroy()
		{
			Services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		protected override void OnInitialized()
		{
			base.OnInitialized();
			
			Services.MessageBrokerService.Subscribe<MenuWorldLootBoxClickedMessage>(OnMenuWorldLootBoxClickedMessage);
		}

		/// <summary>
		/// Called when this screen is Initialised and Opened.
		/// </summary>
		protected override void OnOpened()
		{
			base.OnOpened();
			
			_showNotifications = _gameDataProvider.UniqueIdDataProvider.NewIds.ToList();
			_lootBoxInventoryInfo = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo();

			SetSelectedSlot();
		}

		protected override void OnClosedCompleted()
		{
			_showNotifications.Clear();
		}

		private void UpdateScreen()
		{
			_lootBoxInventoryInfo = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo();
			
			UpdateCratesMenu();
			UpdateSelectedButtonImage();
			UpdatePossibleRarities();
		}

		private void UpdateCratesMenu()
		{
			var list = new List<CratesGridItemView.CratesGridItemViewData>((int)_lootBoxInventoryInfo.SlotCount);

			for (var i = 0; i < _lootBoxInventoryInfo.SlotCount; i++)
			{
				var info = _lootBoxInventoryInfo.TimedBoxSlots[i];
				var viewData = new CratesGridItemView.CratesGridItemViewData
				{
					IsSelected = _selectedSlot == i,
					OnCrateClicked = OnCrateClicked
				};

				if (info.HasValue)
				{
					viewData.Info = info.Value;
					viewData.PlayViewNotificationAnimation = _showNotifications.Contains(viewData.Info.Data.Id);
					viewData.IsEmpty = false;
				}
				else
				{
					viewData.IsEmpty = true;
				}

				list.Add(viewData);
			}
			
			_gridView.UpdateData(list);
			_showNotifications.Clear();
		}

		private async void UpdateSelectedButtonImage()
		{
			var info = _lootBoxInventoryInfo.TimedBoxSlots[_selectedSlot];
			
			_unlockingHolder.SetActive(false);
			_hardCurrencyHolder.SetActive(false);
			_unlockButton.gameObject.SetActive(info.HasValue);

			if (_coroutineTime != null)
			{
				StopCoroutine(_coroutineTime);
				_coroutineTime = null;
			}
			
			if (!info.HasValue)
			{
				_itemTitleText.text = ScriptLocalization.MainMenu.FreeSlot;
				_descriptionText.text = ScriptLocalization.MainMenu.UseCrateSlot;
				_avatarImage.enabled = false;
				_itemsText.enabled = false;
				_crateTierText.enabled = false;
				_hardCurrencyHolder.SetActive(false);

				return;
			}
			
			var boxState = info.Value.GetState(Services.TimeService.DateTimeUtcNow);
			
			_itemTitleText.text = info.Value.Config.LootBoxId.GetTranslation();
			_crateTierText.enabled = true;
			_crateTierText.text = string.Format(ScriptLocalization.MainMenu.CrateTier, info.Value.Config.Tier);
			_avatarImage.enabled = true;
			_avatarImage.sprite = await Services.AssetResolverService.RequestAsset<GameId, Sprite>(info.Value.Config.LootBoxId, false);
			_itemsText.enabled = true;
			_itemsText.text = string.Format(ScriptLocalization.MainMenu.Items, info.Value.Config.ItemsAmount.ToString());
			_descriptionText.text = ScriptLocalization.MainMenu.PossibleRewards;
			
			if (boxState == LootBoxState.Unlocked)
			{
				_unlockButtonText.text = ScriptLocalization.MainMenu.OpenCrate;
			}
			else if (boxState == LootBoxState.Unlocking)
			{
				_unlockButtonText.text = ScriptLocalization.MainMenu.Hurry;
				_hardCurrencyHolder.SetActive(true);
				_coroutineTime = StartCoroutine(UpdateState());
			}
			else
			{
				_unlockButtonText.text = ScriptLocalization.MainMenu.Unlock;
			}
		}

		private void UpdatePossibleRarities()
		{
			var info = _lootBoxInventoryInfo.TimedBoxSlots[_selectedSlot];
			
			foreach (var cardView in _possibleRarityCards)
			{
				cardView.SetInfo(false, "");
			}
			
			if (!info.HasValue)
			{
				_crateContentsHolder.SetActive(false);
				return;
			}
			
			_crateContentsHolder.SetActive(true);
			_possibleRewardsText.enabled = AreAnyGuaranteedDrops(info.Value);
				
			foreach (var cardView in _possibleRarityCards)
			{
				foreach (var rarity in info.Value.PossibleRarities)
				{
					var numCards = GetGuaranteedDropQuantity(info.Value, rarity);

					if (rarity == cardView.Rarity)
					{
						cardView.SetInfo(numCards > 0, $"x{numCards}");
					}
				}
			}
		}

		
		private int GetGuaranteedDropQuantity(TimedBoxInfo info, ItemRarity itemRarity)
		{
			int numCards = 0;

			foreach (var rarity in info.Config.GuaranteeDrop)
			{
				if (rarity == itemRarity)
				{
					numCards++;
				}
			}

			return numCards;
		}
		
		private bool AreAnyGuaranteedDrops(TimedBoxInfo info)
		{
			int numCards = 0;

			foreach (var rarity in info.Config.GuaranteeDrop)
			{
				numCards++;
			}

			return numCards > 0;
		}

		private IEnumerator UpdateState()
		{
			var info = _lootBoxInventoryInfo.TimedBoxSlots[_selectedSlot];
			var time = info.Value.Data.EndTime - Services.TimeService.DateTimeUtcNow;
			var waiter = new WaitForSeconds(1);

			while (time.TotalSeconds > 0)
			{
				_hardCurrencyAmountText.text = info.Value.UnlockCost(Services.TimeService.DateTimeUtcNow).ToString("N0"); 
					
				yield return waiter;
				
				time = info.Value.Data.EndTime - Services.TimeService.DateTimeUtcNow;
			}
		}
		
		private void OnUnlockPressed()
		{
			var info = _lootBoxInventoryInfo.TimedBoxSlots[_selectedSlot];

			if (!info.HasValue)
			{
				return;
			}
			
			var boxState = info.Value.GetState(Services.TimeService.DateTimeUtcNow);
			
			// Open Crate if it's ready.
			if (boxState == LootBoxState.Unlocked)
			{
				LootBoxOpenClicked(info.Value.Data.Id);
				
				return;
			}
			
			var confirmButton = new GenericDialogButton { ButtonText = ScriptLocalization.General.Yes };
			var cost = info.Value.UnlockCost(Services.TimeService.DateTimeUtcNow).ToString("N0"); 

			// Hurry Crate Opening.
			if (boxState == LootBoxState.Unlocking)
			{
				confirmButton.ButtonOnClick = OnHurryComplete;
				
				Services.GenericDialogService.OpenHcDialog(ScriptLocalization.MainMenu.HurryCrate, cost, true, confirmButton);

				return;
			}
			
			// Another crate is already being unlocked. Open the selected one immediately.
			if (_lootBoxInventoryInfo.LootBoxUnlocking.HasValue)
			{
				confirmButton.ButtonOnClick = OnOpenImmediateComplete;

				var stringTitle = string.Format(ScriptLocalization.MainMenu.UnlockCrateNow,
					info.Value.Config.LootBoxId.GetTranslation());
				
				Services.GenericDialogService.OpenHcDialog(stringTitle, cost, true, confirmButton);
			}
			// Start Unlock
			else
			{
				var titleString = string.Format(ScriptLocalization.MainMenu.StartUnlock, info.Value.Config.LootBoxId.GetTranslation());
				confirmButton.ButtonOnClick = StartUnlocking;
				
				Services.GenericDialogService.OpenDialog(titleString, true, confirmButton);
			}
		}
		
		private void StartUnlocking()
		{
			var info = _lootBoxInventoryInfo.TimedBoxSlots[_selectedSlot];
			
			Services.CommandService.ExecuteCommand( new StartUnlockingLootBoxCommand { LootBoxId = info.Value.Data.Id} );
			UpdateScreen();
		}

		private void OnHurryComplete()
		{
			var info = _lootBoxInventoryInfo.TimedBoxSlots[_selectedSlot];

			if (_gameDataProvider.CurrencyDataProvider.Currencies[GameId.HC] < info.Value.UnlockCost(Services.TimeService.DateTimeUtcNow))
			{
				ShowNotEnoughGemsDialog();
			}
			else
			{
				Services.CommandService.ExecuteCommand(new SpeedUpLootBoxCommand { LootBoxId = info.Value.Data.Id });
				UpdateScreen();
			}
		}

		private void OnOpenImmediateComplete()
		{
			var info = _lootBoxInventoryInfo.TimedBoxSlots[_selectedSlot];

			if (_gameDataProvider.CurrencyDataProvider.Currencies[GameId.HC] < info.Value.UnlockCost(Services.TimeService.DateTimeUtcNow))
			{
				ShowNotEnoughGemsDialog();
			}
			else
			{
				Services.CommandService.ExecuteCommand(new SpeedUpLootBoxCommand { LootBoxId = info.Value.Data.Id });
				LootBoxOpenClicked(info.Value.Data.Id);
			}
		}

		private void ShowNotEnoughGemsDialog()
		{
			var titleString = ScriptLocalization.General.NotEnoughGems;
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = Services.GenericDialogService.CloseDialog
			};

			Services.GenericDialogService.OpenDialog(titleString, false, confirmButton);
		}
		
		private void OnBackButtonPressed()
		{
			Data.OnCloseClicked();
		}

		private void OnCrateClicked(int slot)
		{
			if (slot == _selectedSlot)
			{
				OnUnlockPressed();
				UpdateScreen();
				return;
			}
			
			var info = _lootBoxInventoryInfo.TimedBoxSlots[slot];
			
			_selectedSlot = slot;

			// Remove Notifications.
			if (info.HasValue)
			{
				_showNotifications.Remove(info.Value.Data.Id);
				Services.MessageBrokerService.Publish(new CrateClickedMessage{ LootBoxId = info.Value.Data.Id});
			}
			
			UpdateScreen();
		}

		private void LootBoxOpenClicked(UniqueId id)
		{
			Data.LootBoxOpenClicked(id);
			
			_lootBoxInventoryInfo = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo();

			SetSelectedSlot();
		}

		private void OnMenuWorldLootBoxClickedMessage(MenuWorldLootBoxClickedMessage message)
		{
			if (IsOpenedComplete)
			{
				OnUnlockPressed();
			}
		}

		private void SetSelectedSlot()
		{
			_selectedSlot = -1;
			
			if (_lootBoxInventoryInfo.MainLootBox.HasValue)
			{
				OnCrateClicked(_lootBoxInventoryInfo.MainLootBox.Value.Data.Slot);

				return;
			}

			foreach (var boxSlot in _lootBoxInventoryInfo.TimedBoxSlots)
			{
				if (boxSlot.HasValue)
				{
					OnCrateClicked(boxSlot.Value.Data.Slot);
					return;
				}
			}
			
			// There are no crates to select so just update the screen
			_selectedSlot = 0;
			
			UpdateScreen();
		}
	}
}