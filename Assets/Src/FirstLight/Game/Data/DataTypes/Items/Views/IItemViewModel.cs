using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Holds an displayable item, it uses the GameId to get the sprite
	/// </summary>
	public interface IItemViewModel
	{
		/// <summary>
		/// Used to load the sprite and also get the translated name
		/// </summary>
		GameId GameId { get; }

		/// <summary>
		/// Amount displayed at the views
		/// </summary>
		uint Amount { get; }

		/// <summary>
		/// Used in the views to display the item name
		/// </summary>
		string DisplayName { get; }
		
		/// <summary>
		/// Gets a generated item card
		/// </summary>
		VisualElement ItemCard { get; }
		
		/// <summary>
		/// Legacy way of rendering an item sprite.
		/// Should be replaced by item card and removed.
		/// </summary>
		void LegacyRenderSprite(VisualElement icon, Label name, Label amount);
	}

	public static class ItemViewExtensions
	{
		public static IItemViewModel GetViewModel(this ItemData item)
		{
			if (item.Id.IsInGroup(GameIdGroup.Core)) return new CoreItemViewModel(item);
			if (item.HasMetadata<EquipmentMetadata>()) return new EquipmentItemViewModel(item);
			if (item.HasMetadata<CurrencyMetadata>()) return new CurrencyItemViewModel(item);
			if (item.HasMetadata<UnlockMetadata>()) return new UnlockItemViewModel(item);
			if (item.Id.IsInGroup(GameIdGroup.Collection)) return new CollectionViewModel(item);
			FLog.Error($"Not implemented view for item {item}");
			return new CollectionViewModel(item);
		}
	}
}