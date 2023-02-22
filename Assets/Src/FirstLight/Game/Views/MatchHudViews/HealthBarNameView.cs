using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// This View holds the information to show the actor's name 
	/// </summary>
	public class HealthBarNameView : MonoBehaviour
	{
		public Color SquadNameColor;
		public TextMeshProUGUI NameText;

		/// <summary>
		/// Sets the color of the text to the "in squad" one.
		/// </summary>
		public void EnableSquadMode()
		{
			NameText.color = SquadNameColor;
		}
	}
}