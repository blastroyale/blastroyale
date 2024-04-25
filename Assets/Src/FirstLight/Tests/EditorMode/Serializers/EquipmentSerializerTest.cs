using System;
using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Serializers;
using FirstLight.Server.SDK.Modules;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Models
{
	public class EquipmentSerializerTest
	{
		private JsonSerializerSettings _settings;

		private string EquipmentObjectJson = @"{
      ""Adjective"": ""Regular"",
      ""Edition"": ""Genesis"",
      ""Faction"": ""Order"",
      ""GameId"": ""ApoSMG"",
      ""Generation"": 666,
      ""Grade"": ""GradeV"",
      ""InitialReplicationCounter"": 667,
      ""LastRepairTimestamp"": 638173586221573100,
      ""Level"": 668,
      ""Material"": ""Plastic"",
      ""MaxDurability"": 669,
      ""Rarity"": ""Common"",
      ""ReplicationCounter"": 670,
      ""TotalRestoredDurability"": 671,
      ""Tuning"": 672
    }";

		private Equipment ValidEquipment = new()
		{
			GameId = GameId.ApoSMG,
			Adjective = EquipmentAdjective.Regular,
			Edition = EquipmentEdition.Genesis,
			Faction = EquipmentFaction.Order,
			Generation = 666,
			Grade = EquipmentGrade.GradeV,
			InitialReplicationCounter = 667,
			LastRepairTimestamp = 638173586221573100,
			Level = 668,
			Material = EquipmentMaterial.Plastic,
			MaxDurability = 669,
			Rarity = EquipmentRarity.Common,
			ReplicationCounter = 670,
			TotalRestoredDurability = 671,
			Tuning = 672
		};

		[SetUp]
		public void Setup()
		{
			_settings = new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore,
				Converters = new List<JsonConverter>()
				{
					new StringEnumConverter(),
					new EquipmentSerializer(),
				}
			};
		}


		[Test]
		public void TestDeserializeOldStuff()
		{
			var value = JsonConvert.DeserializeObject<Equipment>(EquipmentObjectJson, _settings);
			Assert.AreEqual(ValidEquipment, value);
		}

		[Test]
		public void SerializeIntoArray()
		{
			var value = JsonConvert.SerializeObject(ValidEquipment, _settings);
			Console.WriteLine(value);
			var deserializeObject = JsonConvert.DeserializeObject<Equipment>(value, _settings);
			Assert.AreEqual(ValidEquipment, deserializeObject);
		}

		[Test]
		public void OldEquipmentDataToNew()
		{
			var oldRawJson = "{\"Inventory\":{\"1\":{\"Adjective\":\"Regular\",\"Edition\":\"Genesis\",\"Faction\":\"Order\",\"GameId\":\"ApoSMG\",\"Generation\":0,\"Grade\":\"GradeV\",\"InitialReplicationCounter\":0,\"LastRepairTimestamp\":638173437215128600,\"Level\":1,\"Material\":\"Plastic\",\"MaxDurability\":2,\"Rarity\":\"Common\",\"ReplicationCounter\":0,\"TotalRestoredDurability\":0,\"Tuning\":0},\"2\":{\"Adjective\":\"Regular\",\"Edition\":\"Genesis\",\"Faction\":\"Chaos\",\"GameId\":\"SoldierHelmet\",\"Generation\":0,\"Grade\":\"GradeV\",\"InitialReplicationCounter\":0,\"LastRepairTimestamp\":638173437215128700,\"Level\":1,\"Material\":\"Plastic\",\"MaxDurability\":1,\"Rarity\":\"Common\",\"ReplicationCounter\":0,\"TotalRestoredDurability\":0,\"Tuning\":0},\"3\":{\"Adjective\":\"Regular\",\"Edition\":\"Genesis\",\"Faction\":\"Order\",\"GameId\":\"ApoSMG\",\"Generation\":0,\"Grade\":\"GradeV\",\"InitialReplicationCounter\":0,\"LastRepairTimestamp\":638173498416060000,\"Level\":1,\"Material\":\"Plastic\",\"MaxDurability\":2,\"Rarity\":\"Common\",\"ReplicationCounter\":0,\"TotalRestoredDurability\":0,\"Tuning\":0},\"4\":{\"Adjective\":\"Regular\",\"Edition\":\"Genesis\",\"Faction\":\"Chaos\",\"GameId\":\"SoldierHelmet\",\"Generation\":0,\"Grade\":\"GradeV\",\"InitialReplicationCounter\":0,\"LastRepairTimestamp\":638173498416060200,\"Level\":1,\"Material\":\"Plastic\",\"MaxDurability\":1,\"Rarity\":\"Common\",\"ReplicationCounter\":0,\"TotalRestoredDurability\":0,\"Tuning\":0},\"5\":{\"Adjective\":\"Regular\",\"Edition\":\"Genesis\",\"Faction\":\"Order\",\"GameId\":\"TikTokAmulet\",\"Generation\":0,\"Grade\":\"GradeV\",\"InitialReplicationCounter\":0,\"LastRepairTimestamp\":638174273064706000,\"Level\":1,\"Material\":\"Plastic\",\"MaxDurability\":1,\"Rarity\":\"Common\",\"ReplicationCounter\":0,\"TotalRestoredDurability\":0,\"Tuning\":0},\"6\":{\"Adjective\":\"Regular\",\"Edition\":\"Genesis\",\"Faction\":\"Order\",\"GameId\":\"WarriorArmor\",\"Generation\":0,\"Grade\":\"GradeV\",\"InitialReplicationCounter\":0,\"LastRepairTimestamp\":638174276147990500,\"Level\":1,\"Material\":\"Plastic\",\"MaxDurability\":1,\"Rarity\":\"Common\",\"ReplicationCounter\":0,\"TotalRestoredDurability\":0,\"Tuning\":0}},\"NftInventory\":{},\"LastUpdateTimestamp\":0}";
			var newRawJson = "{\"Inventory\":{\"1\":[0,\"ApoSMG\",0,0,0,4,0,0,0,0,\"638173437215128600\",1,2,0,0,0],\"2\":[0,\"SoldierHelmet\",0,0,1,4,0,0,0,0,\"638173437215128700\",1,1,0,0,0],\"3\":[0,\"ApoSMG\",0,0,0,4,0,0,0,0,\"638173498416060000\",1,2,0,0,0],\"4\":[0,\"SoldierHelmet\",0,0,1,4,0,0,0,0,\"638173498416060200\",1,1,0,0,0],\"5\":[0,\"TikTokAmulet\",0,0,0,4,0,0,0,0,\"638174273064706000\",1,1,0,0,0],\"6\":[0,\"WarriorArmor\",0,0,0,4,0,0,0,0,\"638174276147990500\",1,1,0,0,0]},\"NftInventory\":{},\"LastUpdateTimestamp\":0}";
			FLGCustomSerializers.RegisterSerializers();
			var oldDeserialized = ModelSerializer.Deserialize<EquipmentData>(oldRawJson);
			var newDeserialized = ModelSerializer.Deserialize<EquipmentData>(newRawJson);

			
			Assert.AreEqual(oldDeserialized.Inventory.Count, newDeserialized.Inventory.Count);

			foreach (var (key,equipment) in oldDeserialized.Inventory)
			{
				Assert.AreEqual(equipment,newDeserialized.Inventory[key]);
				
			}

			// Assert that when we serialize the old one will use the new format
			var oldReserialized = ModelSerializer.Serialize(oldDeserialized).Value;
			Assert.AreEqual(newRawJson, oldReserialized);
		}
	}
}