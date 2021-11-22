using DG.Tweening;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Used to display the possible rarity rewards in the form of cards from a Crate on the Crates Screen.
	/// </summary>
	public class PossibleRarityCardView : MonoBehaviour
	{
		public ItemRarity Rarity;
		
		[SerializeField] private TextMeshProUGUI _possibleItemsText;
		[SerializeField] private GameObject _holder;

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
