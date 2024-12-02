using FirstLight.FLogger;
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
		/// Should return the item type display name
		/// </summary>
		string ItemTypeDisplayName { get; }

		/// <summary>
		/// Gets a generated item card
		/// </summary>
		VisualElement ItemCard { get; }

		/// <summary>
		/// Base Item used to generate the view model
		/// </summary>
		ItemData Item { get; }

		/// <summary>
		/// Legacy way of rendering an item sprite.
		/// Should be replaced by item card and removed.
		/// </summary>
		void DrawIcon(VisualElement icon);

		/// <summary>
		/// Displays the quantity of an item or null if nothing to display
		/// </summary>
		string Description { get; }
	}

	public static class ItemViewExtensions
	{
		public static bool TryGetViewModel(this ItemData item, out IItemViewModel itemViewModel)
		{
			if (item.Id.IsInGroup(GameIdGroup.Core))
			{
				itemViewModel = new CoreItemViewModel(item);
			}
			else if (item.HasMetadata<EquipmentMetadata>())
			{
				itemViewModel = new EquipmentItemViewModel(item);
			}
			else if (item.HasMetadata<CurrencyMetadata>())
			{
				itemViewModel = new CurrencyItemViewModel(item);
			}
			else if (item.HasMetadata<UnlockMetadata>())
			{
				itemViewModel = new UnlockItemViewModel(item);
			}
			else if (item.Id.IsInGroup(GameIdGroup.ProfilePicture))
			{
				itemViewModel = new ProfilePictureViewModel(item);
			}
			else if (item.Id.IsInGroup(GameIdGroup.Collection))
			{
				itemViewModel = new CollectionViewModel(item);
			}
			else
			{
				itemViewModel = null;
				return false;
			}

			return true;
		}

		public static IItemViewModel GetViewModel(this ItemData item)
		{
			if (TryGetViewModel(item, out var model))
			{
				return model;
			}

			FLog.Error($"Not implemented view for item {item}");
			return new CollectionViewModel(item);
		}

		public static string GetDisplayName(this ItemData data)
		{
			if (!data.Id.IsInGroup(GameIdGroup.GenericCollectionItem)) return data.Id.GetLocalization();

			if (data.Id == GameId.AvatarRemote)
			{
				return "Avatar";
			}

			// For generic items we cant depend on the game id, so for now display the collection type like "Corpos"
			if (data.TryGetMetadata<CollectionMetadata>(out var metadata) &&
				metadata.TryGetTrait(CollectionTraits.NFT_COLLECTION, out var collection))
			{
				if (collection.Length > 0)
				{
					return collection[0].ToString().ToUpperInvariant() + collection[1..].ToLowerInvariant();
				}

				return collection;
			}

			return "";
		}
	}
}