using UnityEngine;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using UnityEngine.UI;
using System.Collections;

namespace FirstLight.Game.Views.GridViews
{
	/// <summary>
	/// Generic Data container for each grid element on the <see cref="GridAdapterBase{T, TView}"/>
	/// </summary>
	/// <typeparam name="T"> The view type <see cref="IGridItemBase"/> to be shown on the UIa </typeparam>
	public abstract class CellViewsHolderBase<T> : CellViewsHolder where T : IGridItemBase
	{
		protected T View;

		/// <summary>
		/// Retrieving the views from the item's root GameObject
		/// </summary>
		public override void CollectViews()
		{
			// CollectViews();
			View = root.GetComponent<T>();

			rootLayoutElement = root.GetComponent<LayoutElement>();

			views = root;
		}

		/// <summary>
		/// Updates the Cell View because the grid was changed/updated
		/// </summary>
		public void UpdateView(IList data)
		{
			OnUpdateView(data);
			View.UpdateItem(data, ItemIndex);
		}

		protected virtual void OnUpdateView(IList data) {}
		protected virtual void OnCollectViews()	{}
	}
}


