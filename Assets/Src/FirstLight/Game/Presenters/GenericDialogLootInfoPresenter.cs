using System;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using I2.Loc;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <inheritdoc />
	public class GenericDialogLootInfoPresenter : GenericDialogPresenterBase
	{
		[SerializeField] private TextMeshProUGUI _itemsText;
		[SerializeField] private PossibleRarityCardView [] _possibleRarityCards;
		[SerializeField] private Image _lootBoxImage;
		[SerializeField] private GameObject _possibleRewardsHolder;
		
		/// <summary>
		/// Shows the Generic Dialog PopUp with the given <paramref name="title"/> and the <paramref name="button"/> information.
		/// If the given <paramref name="showCloseButton"/> is true, then will show the close button icon on the dialog.
		/// Optionally if defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		public async void SetInfo(GenericDialogButton button, LootBoxInfo boxInfo, Action closeCallback = null)
		{
			var crateText = boxInfo.Config.Tier.ToString();
			var showText = string.Format(ScriptLocalization.MainMenu.CrateTierType, boxInfo.Config.LootBoxId.GetTranslation(), crateText);
			SetBaseInfo(showText, false, button, closeCallback);
			UpdatePossibleRarities(boxInfo);
			
			_lootBoxImage.sprite = await Services.AssetResolverService.RequestAsset<GameId, Sprite>(boxInfo.Config.LootBoxId);
		}
		
		private void UpdatePossibleRarities(LootBoxInfo info)
		{
			_itemsText.text = string.Format(ScriptLocalization.MainMenu.Items, info.Config.ItemsAmount);

			if (AreAnyGuaranteedDrops(info))
			{
				_possibleRewardsHolder.SetActive(true);
				
				foreach (var cardView in _possibleRarityCards)
				{
					cardView.SetInfo(false, "");
				
					foreach (var rarity in info.PossibleRarities)
					{
						if (rarity == cardView.Rarity)
						{
							var numCards = GetGuaranteedDropQuantity(info, rarity);
							
							cardView.SetInfo(numCards > 0, $"x{numCards}");
						}
					}
				}
			}
			else
			{
				_possibleRewardsHolder.SetActive(false);
			}
		}

		private int GetGuaranteedDropQuantity(LootBoxInfo info, ItemRarity itemRarity)
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

		private bool AreAnyGuaranteedDrops(LootBoxInfo info)
		{
			int numCards = 0;

			foreach (var rarity in info.Config.GuaranteeDrop)
			{
				numCards++;
			}

			return numCards > 0;
		}
		
		
	}
}

