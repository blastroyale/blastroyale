using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// Shows loot boxes (rarity, quantity) collected during an adventure on the UI. 
	/// </summary>
	public class PickedUpLootView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _quantityText;
		[SerializeField] private DOTweenAnimation _doTweenAnimation;
		[SerializeField] private Image _lootBoxImage;

		private int _quantity;

		private void Awake()
		{
			_lootBoxImage.enabled = false;
			_quantityText.enabled = false;
			_quantity = 0;
		}

		/// <summary>
		/// Called when Loot is picked up by the player. Will set the Loot icon on the Hud to enabled and play a tween on it 
		/// to notify the player they have collected loot.
		/// </summary>
		public void PickupLoot(int quantity)
		{
			_quantity += quantity;
			_lootBoxImage.enabled = true;
			_quantityText.enabled = true;
			_quantityText.text = $"x{_quantity.ToString()}";
			
			_doTweenAnimation.tween.Complete();
			_doTweenAnimation.tween.Rewind();
			_doTweenAnimation.tween.Play();
		}
	}
}
