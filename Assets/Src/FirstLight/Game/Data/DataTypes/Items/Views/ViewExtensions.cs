using System;
using System.Linq;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	public static class ViewExtensions
	{
		public static string GetCollectionCategoryDisplayName(this CollectionCategory category)
		{
			return category.Id.GetGameIdGroupLocalization();
		}

		/// <summary>
		/// Gets the collection category of a given item. This item needs to be a collection item.
		/// </summary>
		public static CollectionCategory GetCollectionCategory(this ItemData item)
		{
			if (!item.Id.IsInGroup(GameIdGroup.Collection)) throw new Exception($"Item {item} is not a collection item");
			return new (item.Id.GetGroups().First()); // TODO: this is shit
		}
	}
}