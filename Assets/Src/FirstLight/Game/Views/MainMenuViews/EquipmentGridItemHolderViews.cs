using Com.TheFallenGames.OSA.CustomAdapters.GridView;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This class keeps references to an item's views.
	/// Your views holder should extend BaseItemViewsHolder for ListViews and CellViewsHolder for GridViews
	/// The cell views holder should have a single child (usually named "Views"), which contains the actual 
	/// UI elements. A cell's root is never disabled - when a cell is removed, only its "views" GameObject will be disabled
	/// </summary>
	public class EquipmentGridItemHolderViews : CellViewsHolder
	{
		public EquipmentGridItemView EquipmentItemView { get; private set; }

		/// <summary>
		/// Retrieving the views from the item's root GameObject
		/// </summary>
		public override void CollectViews()
		{
			base.CollectViews();

			EquipmentItemView = views.GetComponentInChildren<EquipmentGridItemView>(true);
		}

		/// <summary>
		/// Override this if you have children layout groups. They need to be marked for rebuild when this callback is fired
		/// </summary>
		public override void MarkForRebuild()
		{
			base.MarkForRebuild();
		}
	}

}
