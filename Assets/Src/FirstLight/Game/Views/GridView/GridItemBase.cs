using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FirstLight.Game.Views.GridViews
{
	/// <summary>
	///  Base class used to populate Grid's and Collect their views.
	///  This should be overridden and impelemented in another class, e.g. the Equipment Card.
	/// </summary>
	public interface IGridItemBase
	{
		/// <summary>
		/// Returns the numeric index of this base element within the scroll view it's part of.
		/// </summary>
		int Index { get; }

		/// <summary>
		/// This is called when the GridItem is updated due to the player changing the view, e.g. scrolling to display more elements. Update the data in each of the elements on screen.
		/// </summary>
		void UpdateItem(IList data, int itemIndex);
	}

	/// <inheritdoc cref="IGridItemBase"/>
	public abstract class GridItemBase<T> : MonoBehaviour, IGridItemBase where T : struct
	{
		/// <inheritdoc/>
		public int Index { get; private set; }
		
		/// <summary>
		/// Requests the data representing this grid item
		/// </summary>
		public T Data { get; private set; }

		/// <inheritdoc/>
		public void UpdateItem(IList data, int itemIndex)
		{
			var dataList = data as IList<T>;

			Index = itemIndex;
			Data = dataList[itemIndex];

			OnUpdateItem(Data);
		}

		/// <summary>
		/// This method is implemented by each method that extends this class. Updates the item with the appropriate data. E.g. an Equipment Card sets the name, level of equipment here.
		/// </summary>
		/// <param name="data"></param>
		protected abstract void OnUpdateItem(T data);
	}
}


