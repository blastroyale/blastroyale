using FirstLight.UiService;
using UnityEngine;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Adventure World HUD UI by:
	/// - Showing the World entities HUD visual status
	/// </summary>
	public class AdventureWorldHudPresenter : UiPresenter
	{
		[SerializeField] public Transform _healthBarContainer;
	}
}
