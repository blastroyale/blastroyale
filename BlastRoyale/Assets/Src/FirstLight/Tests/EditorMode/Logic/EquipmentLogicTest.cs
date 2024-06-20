using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using NSubstitute;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;
using Equipment = Quantum.Equipment;
using QuantumBaseEquipmentStatConfig = Quantum.QuantumBaseEquipmentStatConfig;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class EquipmentLogicTest : MockedTestFixture<EquipmentData>
	{
		private Pair<UniqueId, Equipment> _item;
		private EquipmentLogic _equipmentLogic;

		[SetUp]
		public void Init()
		{
			var mockStatsConfigs = Substitute.For<EquipmentStatConfigs>();
			ConfigsProvider.GetConfig<QuantumBaseEquipmentStatConfig>(Arg.Any<int>()).Returns(new QuantumBaseEquipmentStatConfig());
			ConfigsProvider.GetConfig<QuantumEquipmentStatConfig>(Arg.Any<int>()).Returns(new QuantumEquipmentStatConfig());
			_item = SetupItem(1, GameId.ApoCrossbow);
			_equipmentLogic = new EquipmentLogic(GameLogic, DataService);

			TestData.Inventory.Add(_item.Key, _item.Value);
			mockStatsConfigs.GetConfig(Arg.Do<Equipment>(_ => new QuantumEquipmentStatConfig()));
			InitConfigData(new QuantumGameConfig {NftDurabilityDropDays = 7, NonNftDurabilityDropDays = 7});
			InitConfigData(mockStatsConfigs);
			InitConfigData(new QuantumWeaponConfig {});
			InitConfigData(new ScrapConfig
			{
				Rarity = EquipmentRarity.Epic,
				CoinReward = 6000
			});;
			_equipmentLogic.Init();
		}

		[Test]
		public void AddToInventoryCheck()
		{
			var item = SetupItem(2, GameId.ApoCrossbow);
			var id = _equipmentLogic.AddToInventory(item.Value);

			Assert.AreEqual(2, _equipmentLogic.Inventory.Count);
			Assert.AreEqual(item.Value, _equipmentLogic.Inventory[id]);
		}

		[Test]
		public void AddToInventory_AlreadyInInventory_ThrowsException()
		{
			Assert.Throws<ArgumentException>(() => _equipmentLogic.AddToInventory(_item.Value));
		}

		[Test]
		public void AddToInventory_NotEquipment_ThrowsException()
		{
			var item = SetupItem(12, GameId.Barrel);

			Assert.Throws<LogicException>(() => _equipmentLogic.AddToInventory(item.Value));
		}

		[Test]
		public void SetLoadoutCheck()
		{
			var group = _item.Value.GameId.GetGroups()[0];
			var dic = new Dictionary<GameIdGroup, UniqueId> {{group, _item.Key}};

			_equipmentLogic.SetLoadout(dic);

			Assert.AreEqual(1, _equipmentLogic.Loadout.Count);
			Assert.AreEqual(_item.Key, _equipmentLogic.Loadout[group]);
		}

		[Test]
		public void SetLoadoutCheck_DoubleCall_ReplaceSlot()
		{
			var item = SetupItem(2, _item.Value.GameId);
			var group = _item.Value.GameId.GetGroups()[0];

			TestData.Inventory.Add(item.Key, item.Value);
			_equipmentLogic.SetLoadout(new Dictionary<GameIdGroup, UniqueId> {{group, _item.Key}});
			_equipmentLogic.SetLoadout(new Dictionary<GameIdGroup, UniqueId> {{group, item.Key}});

			Assert.AreEqual(1, _equipmentLogic.Loadout.Count);
			Assert.AreNotEqual(_item.Key, _equipmentLogic.Loadout[group]);
			Assert.AreEqual(item.Key, _equipmentLogic.Loadout[group]);
		}

		[Test]
		public void SetLoadout_ScrapItem_UnequipItem()
		{
			var group = _item.Value.GameId.GetGroups()[0];

			_equipmentLogic.SetLoadout(new Dictionary<GameIdGroup, UniqueId> {{group, _item.Key}});
			_equipmentLogic.Scrap(_item.Key);

			Assert.AreEqual(0, _equipmentLogic.Loadout.Count);
			Assert.AreEqual(0, _equipmentLogic.Inventory.Count);
		}

		[Test]
		public void EquipCheck()
		{
			_equipmentLogic.Equip(_item.Key);

			Assert.True(_equipmentLogic.GetInfo(_item.Key).IsEquipped);
			Assert.AreEqual(1, _equipmentLogic.Loadout.Count);
		}

		[Test]
		public void Equip_WeaponAndGear_Check()
		{
			var gear = SetupItem(2, GameId.MausHelmet);

			TestData.Inventory.Add(gear.Key, gear.Value);

			_equipmentLogic.Equip(_item.Key);
			_equipmentLogic.Equip(gear.Key);

			Assert.True(_equipmentLogic.GetInfo(_item.Key).IsEquipped);
			Assert.True(_equipmentLogic.GetInfo(gear.Key).IsEquipped);
			Assert.AreEqual(2, _equipmentLogic.Loadout.Count);
		}

		[Test]
		public void Equip_SlotAlreadyEquipped_Replace()
		{
			var item = SetupItem(2, _item.Value.GameId);

			TestData.Inventory.Add(item.Key, item.Value);

			_equipmentLogic.Equip(item.Key);
			_equipmentLogic.Equip(_item.Key);

			Assert.False(_equipmentLogic.GetInfo(item.Key).IsEquipped);
			Assert.True(_equipmentLogic.GetInfo(_item.Key).IsEquipped);
			Assert.AreEqual(1, _equipmentLogic.Loadout.Count);
		}

		[Test]
		public void Equip_NotInInventory_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _equipmentLogic.Equip(UniqueId.Invalid));
		}

		[Test]
		public void UnequipCheck()
		{
			_equipmentLogic.Equip(_item.Key);
			_equipmentLogic.Unequip(_item.Key);

			Assert.False(_equipmentLogic.GetInfo(_item.Key).IsEquipped);
			Assert.AreEqual(0, _equipmentLogic.Loadout.Count);
		}

		[Test]
		public void Unequip_EmptySlot_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _equipmentLogic.Unequip(_item.Key));
		}

		[Test]
		public void Unequip_NotInInventory_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _equipmentLogic.Unequip(_item.Key));
		}

		[Test]
		public void ScrapItemCheck()
		{
			var reward = _equipmentLogic.Scrap(_item.Key);

			Assert.AreEqual(GameId.COIN, reward.Key);
			Assert.AreEqual(6000, reward.Value);
			Assert.AreEqual(0, _equipmentLogic.Inventory.Count);
		}

		[Test]
		public void ScrapItem_NotInventory_ThrowsException()
		{
			Assert.Throws<KeyNotFoundException>(() => _equipmentLogic.Scrap(UniqueId.Invalid));
		}

		[Test]
		public void ScrapItem_NFTItem_ThrowsException()
		{
			TestData.NftInventory.Add(_item.Key, new NftEquipmentData());

			Assert.Throws<LogicException>(() => _equipmentLogic.Scrap(_item.Key));
		}

		[Test]
		public void UpgradeItemCheck()
		{
			_equipmentLogic.Upgrade(_item.Key);

			Assert.AreEqual(2, _equipmentLogic.Inventory[_item.Key].Level);
		}

		[Test]
		public void UpgradeItem_NotInventory_ThrowsException()
		{
			Assert.Throws<KeyNotFoundException>(() => _equipmentLogic.Upgrade(UniqueId.Invalid));
		}

		[Test]
		public void UpgradeItem_NFTItem_ThrowsException()
		{
			TestData.NftInventory.Add(_item.Key, new NftEquipmentData());

			Assert.Throws<LogicException>(() => _equipmentLogic.Upgrade(_item.Key));
		}

		[Test]
		public void UpgradeItem_MaxLevel_ThrowsException()
		{
			var item = SetupItem(2, GameId.MausHelmet, 1);

			item.Value.Level = (ushort)_equipmentLogic.GetMaxLevel(item.Value);
			TestData.Inventory.Add(item.Key, item.Value);

			Assert.Throws<LogicException>(() => _equipmentLogic.Upgrade(item.Key));
		}
		
		private Pair<UniqueId, Equipment> SetupItem(UniqueId id, GameId gameId, long durabilityTimeStamp = -1)
		{
			var item = new Equipment(gameId)
			{
				Level = 1,
			};
			UniqueIdLogic.Ids[id].Returns(gameId);
			UniqueIdLogic.GenerateNewUniqueId(gameId).Returns(id);

			return new Pair<UniqueId, Equipment>(id, item);
		}
	}
}