using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight;
using FirstLight.Game.Data.DataTypes;
using NSubstitute;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class EquipmentLogicTest : BaseTestFixture<PlayerData>
	{
		/*private const uint _upgradeCost = 10;
		
		private EquipmentData _item;
		private EquipmentLogic _equipmentLogic;

		[SetUp]
		public void Init()
		{
			_item = SetupItem(1,1, 2);
			_equipmentLogic = new EquipmentLogic(GameLogic, DataService);
			
			_equipmentLogic.Init();
		}
		
		[Test]
		public void AddToInventoryCheck()
		{
			_equipmentLogic.AddToInventory(GameId.SC, _item.Level);

			Assert.True(_equipmentLogic.Inventory.Contains(_item));
			Assert.True(TestData.Inventory.Contains(_item));
		}
		
		[Test]
		public void AddToInventory_AlreadyInInventory_ThrowsException()
		{
			_equipmentLogic.AddToInventory(GameId.SC, _item.Level);
			
			Assert.Throws<LogicException>(() => _equipmentLogic.AddToInventory(GameId.SC, _item.Level));
		}
		
		[Test]
		public void AddToInventory_NotEquipment_ThrowsException()
		{
			var item = SetupItem(2, 1, 2, GameId.SC);
			
			Assert.Throws<LogicException>(() => _equipmentLogic.AddToInventory(GameId.SC, _item.Level));
		}
		
		[Test]
		public void EquipCheck()
		{
			TestData.Inventory.Add(_item);
			
			_equipmentLogic.Equip(_item.Id);

			Assert.True(_equipmentLogic.IsEquipped(_item.Id));
			Assert.True(_equipmentLogic.GetEquipmentInfo(_item.Id).IsEquipped);
			Assert.True(TestData.EquippedItems.ContainsValue(_item.Id));
		}
		
		[Test]
		public void Equip_WeaponAndGear_Check()
		{
			var gear = SetupItem(2, 1, 1, GameId.MausHelmet);
			
			TestData.Inventory.Add(_item);
			TestData.Inventory.Add(gear);
			
			_equipmentLogic.Equip(_item.Id);
			_equipmentLogic.Equip(gear.Id);

			Assert.True(_equipmentLogic.IsEquipped(_item.Id));
			Assert.True(_equipmentLogic.GetEquipmentInfo(_item.Id).IsEquipped);
			Assert.True(TestData.EquippedItems.ContainsValue(_item.Id));
			Assert.True(_equipmentLogic.IsEquipped(gear.Id));
			Assert.True(_equipmentLogic.GetEquipmentInfo(gear.Id).IsEquipped);
			Assert.True(TestData.EquippedItems.ContainsValue(gear.Id));
		}
		
		[Test]
		public void Equip_SlotAlreadyEquipped_Replace()
		{
			var item = SetupItem(2);
			
			TestData.Inventory.Add(item);
			TestData.Inventory.Add(_item);
			
			_equipmentLogic.Equip(item.Id);
			_equipmentLogic.Equip(_item.Id);

			Assert.False(_equipmentLogic.IsEquipped(item.Id));
			Assert.True(_equipmentLogic.IsEquipped(_item.Id));
			Assert.False(_equipmentLogic.GetEquipmentInfo(item.Id).IsEquipped);
			Assert.True(_equipmentLogic.GetEquipmentInfo(_item.Id).IsEquipped);
			Assert.False(TestData.EquippedItems.ContainsValue(item.Id));
			Assert.True(TestData.EquippedItems.ContainsValue(_item.Id));
		}
		
		[Test]
		public void Equip_NotInInventory_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _equipmentLogic.Equip(UniqueId.Invalid));
		}
		
		[Test]
		public void UnequipCheck()
		{
			TestData.Inventory.Add(_item);
			TestData.EquippedItems.Add(GameIdGroup.Weapon, _item.Id);
			
			_equipmentLogic.Unequip(_item.Id);

			Assert.False(_equipmentLogic.IsEquipped(_item.Id));
			Assert.False(_equipmentLogic.GetEquipmentInfo(_item.Id).IsEquipped);
			Assert.False(TestData.EquippedItems.ContainsValue(_item.Id));
		}
		
		[Test]
		public void Unequip_EmptySlot_ThrowsException()
		{
			TestData.Inventory.Add(_item);
			
			Assert.Throws<LogicException>(() => _equipmentLogic.Unequip(_item.Id));
		}
		
		[Test]
		public void Unequip_NotInInventory_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _equipmentLogic.Unequip(_item.Id));
		}
		
		[Test]
		public void SellCheck()
		{
			TestData.Inventory.Add(_item);
			
			_equipmentLogic.Sell(_item.Id);

			Assert.False(_equipmentLogic.Inventory.Contains(_item));
			Assert.False(TestData.Inventory.Contains(_item));
		}
		
		[Test]
		public void Sell_EquippedItem_UnequipsFirst()
		{
			TestData.Inventory.Add(_item);
			TestData.EquippedItems.Add(GameIdGroup.Weapon, _item.Id);
			
			_equipmentLogic.Sell(_item.Id);

			Assert.False(_equipmentLogic.IsEquipped(_item.Id));
			Assert.False(TestData.EquippedItems.ContainsValue(_item.Id));
		}
		
		[Test]
		public void Sell_NotInInventory_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _equipmentLogic.Sell(_item.Id));
		}
		
		[Test]
		public void UpgradeCheck()
		{
			TestData.Inventory.Add(_item);
			
			_equipmentLogic.Upgrade(_item.Id);

			Assert.AreEqual(2, TestData.Inventory[0].Level);
			Assert.AreEqual(2, _equipmentLogic.GetEquipmentInfo(_item.Id).CurrentLevel);
		}
		
		[Test]
		public void Upgrade_AlreadyMaxLevel_ThrowsException()
		{
			var item = SetupItem(2);
			
			TestData.Inventory.Add(item);
			
			Assert.Throws<LogicException>(() => _equipmentLogic.Upgrade(item.Id));
		}
		
		[Test]
		public void Upgrade_NotInInventory_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _equipmentLogic.Upgrade(_item.Id));
		}

		private EquipmentData SetupItem(UniqueId id, uint level = 1, int maxLevel = 1, GameId gameId = GameId.AssaultRifle)
		{
			var item = new EquipmentData { Id = id, Level = level };
			var equipmentConfig = new EquipmentConfig
			{
				Id = gameId,
				UpgradePrice = new List<uint>(maxLevel),
				SalePrice = new List<uint>(maxLevel)
			};
			var weaponConfig = new QuantumWeaponConfig
			{
				Id = gameId,
				AttackCooldown = FP._0,
				Damage = new List<uint>(maxLevel)
			};
			var gearConfig = new QuantumGearConfig
			{
				Id = gameId,
				Speed = new List<FP>(maxLevel),
				Hp = new List<uint>(maxLevel),
				CollisionArmor = new List<uint>(maxLevel),
				ProjectileArmor = new List<uint>(maxLevel),
			};

			for (var i = 0; i < maxLevel; i++)
			{
				equipmentConfig.UpgradePrice.Add(_upgradeCost);
				equipmentConfig.SalePrice.Add(_upgradeCost);
				weaponConfig.Damage.Add(1);
				gearConfig.Speed.Add(FP._0);
				gearConfig.Hp.Add(1);
				gearConfig.CollisionArmor.Add(1);
				gearConfig.ProjectileArmor.Add(1);
			}
			
			UniqueIdLogic.Ids[id].Returns(gameId);
			InitConfigData(x => (int) x.Id , equipmentConfig);
			InitConfigData(x => (int) x.Id , weaponConfig);
			InitConfigData(x => (int) x.Id , gearConfig);

			return item;
		}*/
	}
}