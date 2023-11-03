using System;
using System.Linq;
using I2.Loc;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	public static class ViewExtensions
	{
		public static string GetCollectionCategoryDisplayName(this CollectionCategory category)
		{
			return category.Id switch
			{
				GameIdGroup.Glider         => ScriptLocalization.UITCollectionScreen.gliders,
				GameIdGroup.PlayerSkin     => ScriptLocalization.UITCollectionScreen.characters,
				GameIdGroup.DeathMarker    => ScriptLocalization.UITCollectionScreen.banners,
				GameIdGroup.MeleeSkin      => ScriptLocalization.UITCollectionScreen.meleeskins,
				GameIdGroup.ProfilePicture => ScriptLocalization.UITCollectionScreen.avatars,
				_                          => category.Id.ToString()
			};
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