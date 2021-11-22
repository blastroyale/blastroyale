using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FirstLight.Game.Views.GridViews
{
	/// <summary>
	/// A generic grid view which can be placed onto an OSA object added to a UI Screen.
	/// Allows a large grid list of data that can support almost infinite items. 
	/// </summary>
	public class GenericGridView : GridAdapterBase<GenericCellViewsHolder, IGridItemBase>
	{

		public void Clear()
		{
			ClearVisibleItems();
		}
		
	}
}
