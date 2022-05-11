using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Used to display the possible rarity rewards in the form of cards from a Crate on the Crates Screen.
	/// </summary>
	public class PossibleRarityCardView : MonoBehaviour
	{
		public ItemRarity Rarity;
		
		[SerializeField, Required] private TextMeshProUGUI _possibleItemsText;
		[SerializeField, Required] private GameObject _holder;

		/// <summary>
		/// Sets the rarity card information to be shown to the player
		/// </summary>
		public void SetInfo(bool show, string text)
		{
			_holder.SetActive(show);
			_possibleItemsText.text = text;
		}
	}
}
