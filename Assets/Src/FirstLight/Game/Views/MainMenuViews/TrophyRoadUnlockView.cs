using UnityEngine;
using FirstLight.Game.Configs;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using TMPro;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This class shows new rewards and content earned through the Trophy Road progression system in the game.
	/// </summary>
	public class TrophyRoadUnlockView : MonoBehaviour
	{
		[SerializeField] private Image _image;

		[SerializeField, Required] private Sprite _shopSprite;
		[SerializeField, Required] private Sprite _cratesSprite;
		[SerializeField, Required] private Sprite _fusionSprite;
		[SerializeField, Required] private Sprite _enhanceSprite;
		[SerializeField, Required] private TextMeshProUGUI _newItemsText;
		[SerializeField, Required] private TextMeshProUGUI _newItemNameText;
		[SerializeField, Required] private Image _unlockedImage;
		
		/// <summary>
		/// Set Loot information here; Rarity, Level, Quantity, etc.
		/// </summary>
		public void SetInfo(Transform parent, UnlockSystem unlock, string title, string description, bool unlocked)
		{
			gameObject.transform.SetParent(parent);

			switch (unlock)
			{
				case UnlockSystem.Crates: _image.sprite = _cratesSprite; break;
				case UnlockSystem.Enhancement: _image.sprite = _enhanceSprite; break;
				case UnlockSystem.Fusion: _image.sprite = _fusionSprite; break;
				case UnlockSystem.Shop: _image.sprite = _shopSprite; break;
			}

			_newItemsText.text = title;
			_newItemNameText.text = description;
			_unlockedImage.enabled = unlocked;
		}
	}
}


