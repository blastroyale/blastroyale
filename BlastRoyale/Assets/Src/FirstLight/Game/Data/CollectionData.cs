using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Serializers;
using FirstLight.Game.Utils;
using FirstLight.Models.Collection;
using FirstLight.Server.SDK.Models;
using FirstLightServerSDK.Modules;
using FirstLightServerSDK.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Utilities;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Data
{
	/// <summary>
	/// Known collection traits keys
	/// </summary>
	public static class CollectionTraits
	{
		public static readonly string URL = "url";
		public static readonly string TOKEN_ID = "token";
		public static readonly string NFT_COLLECTION = "nft_collection";
	}

	/// <summary>
	/// Helper class for single allocation of structs
	/// </summary>
	public static class CollectionCategories
	{
		public static readonly CollectionCategory PLAYER_SKINS = new (GameIdGroup.PlayerSkin);
		public static readonly CollectionCategory GLIDERS = new (GameIdGroup.Glider);
		public static readonly CollectionCategory GRAVE = new (GameIdGroup.DeathMarker);
		public static readonly CollectionCategory MELEE_SKINS = new (GameIdGroup.MeleeSkin);
		public static readonly CollectionCategory PROFILE_PICTURE = new (GameIdGroup.ProfilePicture);
	}

	/// <summary>
	/// Holds information of a collection category. Currently mapped to a GameIdGroup.
	/// Should change in the future because game id groups are a pain in the bumbumb as it should be
	/// data driven but its embedded in the code and this burns my soul to death.
	/// </summary>
	[Serializable]
	public readonly struct CollectionCategory : IEqualityComparer<CollectionCategory>, IEquatable<CollectionCategory>
	{
		public readonly GameIdGroup Id;

		public bool IsValid() => Id != GameIdGroup.GameDesign;

		public CollectionCategory(GameIdGroup id)
		{
			Id = id;
		}

		public bool Equals(CollectionCategory x, CollectionCategory y)
		{
			return x.Id == y.Id;
		}

		public static bool operator ==(CollectionCategory obj1, CollectionCategory obj2)
		{
			return obj1.Equals(obj2);
		}

		public static bool operator !=(CollectionCategory obj1, CollectionCategory obj2) => !(obj1 == obj2);

		public override bool Equals(object obj)
		{
			return obj is CollectionCategory other && Equals(other);
		}

		public override int GetHashCode()
		{
			return GetHashCode(this);
		}

		public int GetHashCode(CollectionCategory obj)
		{
			return obj.Id.GetHashCode();
		}

		public bool Equals(CollectionCategory other)
		{
			return Id == other.Id;
		}
	}

	/// <summary>
	/// Metadata item of a given collection.
	/// Here we can store specific collection traits like colors, materials, ids.
	/// They can share the same game id but have different metas.
	/// </summary>
	[Serializable]
	public class CollectionTrait
	{
		public readonly string Key;
		public readonly string Value;

		public CollectionTrait(string key, string value)
		{
			Key = key;
			Value = value;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Key != null ? Key.GetDeterministicHashCode() : 0) * 397) ^ (Value != null ? Value.GetDeterministicHashCode() : 0);
			}
		}
	}

	/// <summary>
	/// Player data holder of what is owned by a given collection or what is equipped.
	/// Some items from the owned can be remotely synced via CollectionItemEnrichmentData
	/// </summary>
	[Serializable]
	public class CollectionData
	{
		[JsonProperty] [JsonConverter(typeof(CustomDictionaryConverter<CollectionCategory, List<ItemData>>))]
		public readonly Dictionary<CollectionCategory, List<ItemData>> OwnedCollectibles = new ();

		[JsonProperty] [JsonConverter(typeof(CustomDictionaryConverter<CollectionCategory, ItemData>))]
		public readonly Dictionary<CollectionCategory, ItemData> Equipped = new ()
		{
			{CollectionCategories.PLAYER_SKINS, ItemFactory.Collection(GameId.PlayerSkinBrandMale)},
		};

		public ulong LastUpdateTimestamp;

		/// <summary>
		/// This is used in the quantum server plugin for validating the skins, if you want to check inside the game do not use this!
		/// </summary>
		/// <param name="gameId"></param>
		/// <returns></returns>
		public bool HasCollectionItem(GameId gameId)
		{
			return OwnedCollectibles.Values.SelectMany(ownedItems => ownedItems)
				.Any(data => data.Id == gameId);
		}

		public override int GetHashCode()
		{
			int hash = 17;
			foreach (var collection in OwnedCollectibles.Values)
			{
				foreach (var item in collection) hash = unchecked(hash * 23 + item.GetHashCode());
			}

			foreach (var item in Equipped.Values) hash = unchecked(hash * 23 + item.GetHashCode());
			hash = unchecked(hash * 37 + LastUpdateTimestamp.GetHashCode());
			return hash;
		}
	}
}