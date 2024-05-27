using System;
using System.Collections.Generic;
using ExitGames.Client.Photon.StructWrapping;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Serializers;
using FirstLight.Server.SDK.Modules;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Models
{
	public class ItemDataTests
	{
		[Test]
		public void TestCurrencyMeta()
		{
			var dataWithAmount = ItemFactory.Currency(GameId.COIN, 100);
			
			Assert.IsTrue(dataWithAmount.TryGetMetadata<CurrencyMetadata>(out var _));
			Assert.AreEqual(ItemMetadataType.Currency, dataWithAmount.MetadataType);
		}
		
		[Test]
		public void TestEquipmentMeta()
		{
			var dataWithAmount = ItemFactory.Equipment(new Equipment());
			
			Assert.IsTrue(dataWithAmount.TryGetMetadata<EquipmentMetadata>(out var _));
			Assert.AreEqual(ItemMetadataType.Equipment, dataWithAmount.MetadataType);
		}
		
		[Test]
		public void TestNoMetadata()
		{
			var dataWithAmount = ItemFactory.Simple(GameId.Barrel);
			
			Assert.AreEqual(ItemMetadataType.None, dataWithAmount.MetadataType);
		}
		
		[Test]
		public void TestCollectionMeta()
		{
			var dataWithAmount = ItemFactory.Collection(GameId.Male01Avatar, new CollectionTrait("key", "value"));
			
			Assert.IsTrue(dataWithAmount.TryGetMetadata<CollectionMetadata>(out var _));
			Assert.AreEqual(ItemMetadataType.Collection, dataWithAmount.MetadataType);
		}
		
		[Test]
		public void TestSerializeDeserializeItemDataWithMetadata()
		{
			var dataWithAmount = ItemFactory.Currency(GameId.COIN, 100);

			var serialized = ModelSerializer.Serialize(dataWithAmount).Value;
			var deserialized = ModelSerializer.Deserialize<ItemData>(serialized);

			var currencyMeta = deserialized.GetMetadata<CurrencyMetadata>();
			
			Assert.AreEqual(100, currencyMeta.Amount);
		}
		
		[Test]
		public void TestDeserializeEquipmentMetadata()
		{
			var eq = new Equipment()
			{
				Adjective = EquipmentAdjective.Magnificent,
				Faction = EquipmentFaction.Organic,
				GameId = GameId.ModPistol,
				Rarity = EquipmentRarity.Rare
			};
			var item = ItemFactory.Equipment(eq);

			var serialized = ModelSerializer.Serialize(item).Value;
			var deserialized = ModelSerializer.Deserialize<ItemData>(serialized);
			var meta = deserialized.GetMetadata<EquipmentMetadata>();
			
			Assert.AreEqual(eq, meta.Equipment);
			Assert.AreEqual(deserialized, item);
		}
		
		[Test]
		public void TestDeserializeCollectionMetadata()
		{
			var trait = new CollectionTrait("token_color", "red");
			var item = ItemFactory.Collection(GameId.Male01Avatar, trait);

			var serialized = ModelSerializer.Serialize(item).Value;
			var deserialized = ModelSerializer.Deserialize<ItemData>(serialized);
			var meta = deserialized.GetMetadata<CollectionMetadata>();
			
			Assert.AreEqual(trait.GetHashCode(), meta.Traits[0].GetHashCode());
			Assert.AreEqual(deserialized, item);
			Assert.AreEqual(trait.Key, deserialized.GetMetadata<CollectionMetadata>().Traits[0].Key);
		}
		
		[Test]
		public void TestSerializeNoMetadata()
		{
			var item = ItemFactory.Simple(GameId.Barrel);

			var serialized = ModelSerializer.Serialize(item).Value;
			var deserialized = ModelSerializer.Deserialize<ItemData>(serialized);

			Assert.AreEqual(deserialized, item);
		}
	}
}