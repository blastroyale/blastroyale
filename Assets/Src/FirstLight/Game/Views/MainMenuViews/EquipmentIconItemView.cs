using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
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
		[SerializeField, Required] private TextMeshProUGUI _levelText;
		[SerializeField, Required] private Image _iconImage;
		[SerializeField, Required] private Image _rarityImage;

		private IGameServices _services;

		/// <summary>
		/// Sets the information for this view
		/// </summary>
		public async void SetInfo(Equipment equipment)
		{
			_services ??= MainInstaller.Resolve<IGameServices>();
			_levelText.text = $"{ScriptLocalization.General.Level} {equipment.Level.ToString()}";
			_iconImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(equipment.GameId);
			_rarityImage.sprite =
				await _services.AssetResolverService.RequestAsset<EquipmentRarity, Sprite>(equipment.Rarity);
		}
	}
}