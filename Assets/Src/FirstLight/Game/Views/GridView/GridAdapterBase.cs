using System.Collections.Generic;
using UnityEngine;
using frame8.Logic.Misc.Other.Extensions;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using System.Collections;

namespace FirstLight.Game.Views.GridViews
{
	/// <summary>
	/// This class handles a grid view of Equipment Items which the player can equip / upgrade, etc.
	/// This uses an imported library, Optimised Scroll View Adapter that is super performant and allows us thousands of items in a scroll view.
	/// </summary>
	public abstract class GridAdapterBase<T, TItem> : GridAdapter<GridParams, T>
		where T : CellViewsHolder, new()
		where TItem : IGridItemBase 
	{
		private SimpleDataHelper<int> _size;

		public IList Data { get; private set; }
		public List<TItem> List { get; private set; }

		protected override void Awake()
		{
			_size = new SimpleDataHelper<int>(this);

			base.Awake();
		}

		/// <summary>
		/// Updates the grid to have the given <paramref name="gridSize"/>
		/// </summary>
		public void UpdateData(IList data)
		{
			Data = data;

			_size.List.Clear();
			_size.List.AddRange(new int[data.Count]);
			_size.NotifyListChangedExternally();
		}

		/// <summary>
		/// This is called anytime a previously invisible item become visible, or after it's created, or when anything that requires a refresh happens.
		/// Here we bind the data from the model to the item's views
		/// </summary>
		protected override void UpdateCellViewsHolder(T newOrRecycled)
		{
			var viewHolder = newOrRecycled as CellViewsHolderBase<TItem>;

			viewHolder.UpdateView(Data);
		}
	}
}
