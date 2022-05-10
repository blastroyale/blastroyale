using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This View handles non real money IAP shop item View in the UI.
	/// </summary>
	public class ShopItemNonRealMoneyView : ShopItemView
	{
		[SerializeField, Required] private Button _infoButton;
		
		private IGameDataProvider _gameDataProvider;
		
		protected override void OnAwake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			_infoButton.onClick.AddListener(OnInfoClick);
		}
		
		protected override void Buy()
		{
			if (Product.Data.PriceValue <= _gameDataProvider.CurrencyDataProvider.GetCurrencyAmount(Product.Data.PriceGameId))
			{
				var titleString = string.Format(ScriptLocalization.Shop.ConfirmPurchase, LocalizationManager.GetTranslation(Product.Metadata.localizedTitle));
	
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.Yes,
					ButtonOnClick = base.Buy
				};
				
				GameServices.GenericDialogService.OpenHcDialog(titleString, Product.Data.PriceValue.ToString(), true, confirmButton, Product.Data.PriceGameId == GameId.SC);
			}
			else
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = GameServices.GenericDialogService.CloseDialog
				};

				GameServices.GenericDialogService.OpenDialog(ScriptLocalization.General.NotEnoughCash, false, confirmButton);
			}
		}
		
		private void OnInfoClick() 
		{ 
			if (!Product.Data.RewardGameId.IsInGroup(GameIdGroup.CoreBox))
			{
				return;
			}
			
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = GameServices.GenericDialogService.CloseDialog
			};

			var coreBoxInfo = _gameDataProvider.LootBoxDataProvider.GetLootBoxInfo((int)Product.Data.RewardValue);
			GameServices.GenericDialogService.OpenLootInfoDialog(confirmButton, coreBoxInfo);
		}
	}
}

