using FirstLight.UiService;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Adventure World HUD UI by:
	/// - Showing the World entities HUD visual status
	/// </summary>
	public class AdventureWorldHudPresenter : UiPresenter
	{
		[SerializeField, Required] public Transform _healthBarContainer;
	}
}
