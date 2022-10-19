using System;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Manages the IAP store.
	/// </summary>
	[LoadSynchronously]
	public class StoreScreenPresenter : UiToolkitPresenterData<StoreScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action BackClicked;
		}

		protected override void QueryElements(VisualElement root)
		{
			root.Q<Button>("BackButton").clicked += Data.BackClicked;
		}
	}
}