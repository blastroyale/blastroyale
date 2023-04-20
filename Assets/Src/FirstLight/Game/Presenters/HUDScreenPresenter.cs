using FirstLight.Game.Views.UITK;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class HUDScreenPresenter : UiToolkitPresenterData<HUDScreenPresenter.StateData>
	{
		public struct StateData
		{
		}


		private WeaponDisplayView _weaponDisplayView;

		protected override void QueryElements(VisualElement root)
		{
			root.Q("WeaponDisplay").AttachView(this, out _weaponDisplayView);
		}
	}
}