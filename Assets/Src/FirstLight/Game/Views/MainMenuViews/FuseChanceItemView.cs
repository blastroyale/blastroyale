using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Used to display the % chance of fusing a new item of the specified type.
	/// </summary>
	public class FuseChanceItemView : MonoBehaviour
	{
		[SerializeField] private GameIdGroup _equipmentGroup;
		[SerializeField, Required] private TextMeshProUGUI _percentageChanceText;

		/// <summary>
		/// Request the 
		/// </summary>
		public GameIdGroup GameIdGroup => _equipmentGroup;
		
		/// <summary>
		/// Sets the % chance based on parameters / logic.
		/// </summary>
		public void SetInfo(uint percentage)
		{
			_percentageChanceText.text =  $"{percentage.ToString()}%";
		}
	}
}
