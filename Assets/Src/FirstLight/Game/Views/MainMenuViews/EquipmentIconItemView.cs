using FirstLight.Game.Configs;
using FirstLight.Game.Infos;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This view is responsible to show the item icons and information
	/// </summary>
	public class EquipmentIconItemView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _levelText;
		[SerializeField] private Image _iconImage;
		[SerializeField] private Image _rarityImage;

		private IGameServices _services;
		
		/// <summary>
		/// Sets the information for this view
		/// </summary>
		public async void SetInfo(EquipmentDataInfo info)
		{
			_services ??= MainInstaller.Resolve<IGameServices>();
			_levelText.text = $"{ScriptLocalization.General.Level} {info.Data.Level.ToString()}";
			_iconImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(info.GameId);
			_rarityImage.sprite = await _services.AssetResolverService.RequestAsset<ItemRarity, Sprite>(info.Data.Rarity);
		}
	}
}