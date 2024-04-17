using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using FirstLight.Game.Configs;
using FirstLight.Server.SDK.Modules;
using Newtonsoft.Json;
using PlayFab.ClientModels;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	public enum ItemMetadataType
	{
		None,
		Equipment,
		Unlock,
		Currency,
		Collection
	}

	public interface IItemMetadata
	{
		[IgnoreDataMember] ItemMetadataType MetaType { get; }
	}

	/// <summary>
	/// Used on server for backwards compatibility
	/// </summary>
	[Serializable]
	public class LegacyItemData
	{
		public GameId RewardId;
		public int Value;
	}

	public static class ItemFactory
	{
		public static ItemData Simple(GameId id) => new (id, null);
		public static ItemData Currency(GameId id, int amt) => new (id, new CurrencyMetadata() {Amount = amt});
		public static ItemData Equipment(Equipment e) => new (e.GameId, new EquipmentMetadata() {Equipment = e});
		public static ItemData Collection(GameId id, params CollectionTrait[] traits) => new (id, new CollectionMetadata() {Traits = traits});
		public static ItemData Collection(GameId id) => new (id, null);
		public static ItemData Unlock(UnlockSystem unlock) => new (GameId.Random, new UnlockMetadata() {Unlock = unlock});

		public static ItemData PlayfabCatalog(CatalogItem catalogItem)
		{
			return Legacy(ModelSerializer.Deserialize<LegacyItemData>(catalogItem.CustomData));
		}

		public static ItemData Legacy(LegacyItemData legacy)
		{
			if (legacy.RewardId.IsInGroup(GameIdGroup.Equipment)) return Equipment(new Equipment(legacy.RewardId));
			if (legacy.RewardId.IsInGroup(GameIdGroup.Collection)) return Collection(legacy.RewardId);
			if (legacy.RewardId.IsInGroup(GameIdGroup.Currency)) return Currency(legacy.RewardId, legacy.Value);
			return new ItemData(legacy.RewardId, legacy.Value > 1 ? new CurrencyMetadata() {Amount = legacy.Value} : null);
		}
	}

	/// <summary>
	/// Generic representation of an awardable item in-game.
	/// This item will have its respective IItemViewData for visual in-game representation
	/// </summary>
	[Serializable]
	public class ItemData : IEquatable<ItemData>, IEqualityComparer<ItemData>
	{
		[JsonProperty] public readonly GameId Id;

		[JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
		private readonly IItemMetadata _metadata;

		[IgnoreDataMember] public ItemMetadataType MetadataType => _metadata?.MetaType ?? ItemMetadataType.None;

		public ItemData()
		{
		} // should not be used except serialization

		public ItemData(GameId id, IItemMetadata meta)
		{
			Id = id;
			_metadata = meta;
		}

		public T GetMetadata<T>() where T : IItemMetadata
		{
			TryGetMetadata<T>(out var m);
			return m;
		}

		public bool HasMetadata<T>() where T : IItemMetadata
		{
			return _metadata is T;
		}

		public bool TryGetMetadata<T>(out T meta) where T : IItemMetadata
		{
			if (_metadata is T unbox)
			{
				meta = unbox;
				return true;
			}

			meta = default;
			return false;
		}

		public bool Equals(ItemData other)
		{
			if (other == null) return false;
			return Id == other?.Id && GetMetadataHashCode() == other?.GetMetadataHashCode();
		}

		public override bool Equals(object obj)
		{
			return obj is ItemData other && Equals(other);
		}


		public bool Equals(ItemData x, ItemData y)
		{
			if (x == null && y == null) return true;
			if (x == null || y == null) return false;
			return x?.Id == y?.Id && x?.GetMetadataHashCode() == y?.GetMetadataHashCode();
		}

		public int GetMetadataHashCode() => _metadata?.GetHashCode() ?? 0;

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = 17;
				hashCode = hashCode * 31 + Id.GetHashCode();
				hashCode = hashCode * 31 + GetMetadataHashCode();
				return hashCode;
			}
		}

		public override string ToString()
		{
			return $"<Item Id={Id} Meta={JsonConvert.SerializeObject(_metadata)}>";
		}

		public int GetHashCode(ItemData obj)
		{
			return obj?.GetHashCode() ?? 0;
		}
	}
}