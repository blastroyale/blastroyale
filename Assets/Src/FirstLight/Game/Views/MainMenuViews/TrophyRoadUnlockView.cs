using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using FirstLight.Game.Configs;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Services;
using I2.Loc;
using Quantum;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This class shows new rewards and content earned through the Trophy Road progression system in the game.
	/// </summary>
	public class TrophyRoadUnlockView : MonoBehaviour
	{
		[SerializeField] private Image _image;

		[SerializeField] private Sprite _shopSprite;
		[SerializeField] private Sprite _cratesSprite;
		[SerializeField] private Sprite _fusionSprite;
		[SerializeField] private Sprite _enhanceSprite;
		[SerializeField] private TextMeshProUGUI _newItemsText;
		[SerializeField] private TextMeshProUGUI _newItemNameText;
		[SerializeField] private Image _unlockedImage;
		
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


