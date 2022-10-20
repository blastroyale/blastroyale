using System;
using FirstLight.FLogger;
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
		private const string ITEM_RARE_ID = "com.firstlight.blastroyale.core.rare";
		private const string ITEM_EPIC_ID = "com.firstlight.blastroyale.core.epic";
		private const string ITEM_LEGENDARY_ID = "com.firstlight.blastroyale.core.legendary";

		public struct StateData
		{
			public Action BackClicked;
			public Action<string> OnPurchaseItem;
		}

		protected override void QueryElements(VisualElement root)
		{
			root.Q<Button>("BackButton").clicked += Data.BackClicked;
			root.Q<Button>("ItemRare").clicked += () => { BuyItem(ITEM_RARE_ID); };
			root.Q<Button>("ItemEpic").clicked += () => { BuyItem(ITEM_EPIC_ID); };
			root.Q<Button>("ItemLegendary").clicked += () => { BuyItem(ITEM_LEGENDARY_ID); };
		}

		private void BuyItem(string id)
		{
			Data.OnPurchaseItem(id);
		}
	}
}