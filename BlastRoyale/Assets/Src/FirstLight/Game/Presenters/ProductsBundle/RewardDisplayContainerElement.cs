using System.Linq;
using ExitGames.Client.Photon.StructWrapping;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using QuickEye.UIToolkit;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters.ProductsBundle
{
	public class RewardDisplayContainerElement : VisualElement 
	{

		[Q] private VisualElement _rewardIcon;
		[Q] private Label _rewardCurrency;
		[Q] private Label _rewardItemCategory; 
		
		private string[] _nonCurrencyItemClasses = new[] {"Avatar", "Character", "Flag", "Melee", "Glider"};
		private string[] _iconModifiersAllowedItemClasses = new[] {"Character"};
		
		public RewardDisplayContainerElement()
		{
			this.LoadTemplateAndBind("TemplateRewardDisplayContainer");
		}

		public RewardDisplayContainerElement SetupContainer(GameProduct bundleProduct)
		{
			_rewardIcon.RemoveModifiers();

			var itemClass = bundleProduct.PlayfabProductConfig.CatalogItem.ItemClass;
			
			if (_nonCurrencyItemClasses.Contains(itemClass))
			{
				if (_iconModifiersAllowedItemClasses.Contains(itemClass))
				{
					_rewardIcon.AddToClassList($"reward-display-icon--{itemClass.ToLowerInvariant()}");
				} 
				
				_rewardCurrency.SetDisplay(false);
			}
			else
			{
				//BundleProduct is supposed to be one type of Currency
				var itemMetaData = bundleProduct.GameItem.GetMetadata<CurrencyMetadata>();

				if (itemMetaData != null)
				{ 
					_rewardIcon.AddToClassList("reward-display-icon--currency");
					_rewardCurrency.text = itemMetaData.Amount.ToString();
				}
				else
				{
					_rewardCurrency.SetDisplay(false);
				}
			}

			var itemView = bundleProduct.GameItem.GetViewModel(); 
			itemView.DrawIcon(_rewardIcon);
			_rewardItemCategory.text = bundleProduct.PlayfabProductConfig.CatalogItem.ItemClass;

			return this;
		}

		public new class UxmlFactory : UxmlFactory<RewardDisplayContainerElement, UxmlTraits>
		{
		}
	}
}