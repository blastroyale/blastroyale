using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using NSubstitute;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class EquipmentLogicTest : MockedTestFixture<EquipmentData>
	{
		private PlayerData _playerData;
		private Pair<UniqueId, Equipment> _item;
		private EquipmentLogic _equipmentLogic;

		[SetUp]
		public void Init()
		{
			var mockStatsConfigs = Substitute.For<EquipmentStatConfigs>();
			
			_item = SetupItem(1, GameId.ApoCrossbow, 1, 2);
			_equipmentLogic = new EquipmentLogic(GameLogic, DataService);
			_playerData = Activator.CreateInstance<PlayerData>();
			
			mockStatsConfigs.GetConfig(Arg.Do<Equipment>(_ => new QuantumEquipmentStatConfig()));
			InitConfigData(mockStatsConfigs);
			InitConfigData(new QuantumWeaponConfig { Specials = new List<GameId> { GameId.SpecialShieldSelf, GameId.SpecialShieldSelf } });
			DataService.GetData<PlayerData>().Returns(x => _playerData);
			_equipmentLogic.Init();
		}
		
		[Test]
		public void AddToInventoryCheck()
		{
			var id = _equipmentLogic.AddToInventory(_item.Value);

			Assert.AreEqual(1, _equipmentLogic.Inventory.Count);
			Assert.AreEqual(_item.Value, _equipmentLogic.Inventory[id]);
		}
		
		[Test]
		public void AddToInventory_AlreadyInInventory_ThrowsException()
		{
			_equipmentLogic.AddToInventory(_item.Value);
			
			Assert.Throws<ArgumentException>(() => _equipmentLogic.AddToInventory(_item.Value));
		}
		
		[Test]
		public void AddToInventory_NotEquipment_ThrowsException()
		{
			var item = SetupItem(1, GameId.Barrel, 1, 2);
			
			Assert.Throws<LogicException>(() => _equipmentLogic.AddToInventory(item.Value));
		}
		
		[Test]
		public void RemoveFromInventoryCheck()
		{
			TestData.Inventory.Add(_item.Key, _item.Value);
			//TestData.InsertionTimestamps.Add(_item.Key, 0);

			Assert.True(_equipmentLogic.RemoveFromInventory(_item.Key));
			Assert.AreEqual(0, _equipmentLogic.Inventory.Count);
		}
		
		[Test]
		public void SetLoadoutCheck()
		{
			var group = _item.Value.GameId.GetGroups()[0];
			var dic = new Dictionary<GameIdGroup, UniqueId> { { group, _item.Key } };
			
			TestData.Inventory.Add(_item.Key, _item.Value);
			//TestData.InsertionTimestamps.Add(_item.Key, 0);
			_equipmentLogic.SetLoadout(dic);

			Assert.AreEqual(1, _equipmentLogic.Loadout.Count);
			Assert.AreEqual(_item.Key, _equipmentLogic.Loadout[group]);
		}
		
		[Test]
		public void SetLoadoutCheck_DoubleCall_ReplaceSlot()
		{
			var item = SetupItem(2, _item.Value.GameId);
			var group = _item.Value.GameId.GetGroups()[0];
			
			TestData.Inventory.Add(_item.Key, _item.Value);
			TestData.Inventory.Add(item.Key, item.Value);
			//TestData.InsertionTimestamps.Add(_item.Key, 0);
			//TestData.InsertionTimestamps.Add(item.Key, 0);
			_equipmentLogic.SetLoadout(new Dictionary<GameIdGroup, UniqueId> { { group, _item.Key } });
			_equipmentLogic.SetLoadout(new Dictionary<GameIdGroup, UniqueId> { { group, item.Key } });

			Assert.AreEqual(1, _equipmentLogic.Loadout.Count);
			Assert.AreNotEqual(_item.Key, _equipmentLogic.Loadout[group]);
			Assert.AreEqual(item.Key, _equipmentLogic.Loadout[group]);
		}
		
		[Test]
		public void SetLoadout_RemoveFromInventory_UnequipItem()
		{
			var dic = new Dictionary<GameIdGroup, UniqueId> { { _item.Value.GameId.GetGroups()[0], _item.Key } };
			
			TestData.Inventory.Add(_item.Key, _item.Value);
			//TestData.InsertionTimestamps.Add(_item.Key, 0);
			_equipmentLogic.SetLoadout(dic);

			Assert.True(_equipmentLogic.RemoveFromInventory(_item.Key));
			Assert.AreEqual(0, _equipmentLogic.Inventory.Count);
		}
		
		[Test]
		public void RemoveFromInventory_NotInventory_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _equipmentLogic.RemoveFromInventory(UniqueId.Invalid));
		}
		
		[Test]
		public void EquipCheck()
		{
			TestData.Inventory.Add(_item.Key, _item.Value);
			//TestData.InsertionTimestamps.Add(_item.Key, 0);
			
			_equipmentLogic.Equip(_item.Key);

			Assert.True(_equipmentLogic.GetInfo(_item.Key).IsEquipped);
			Assert.True(_playerData.Equipped.ContainsValue(_item.Key));
		}
		
		[Test]
		public void Equip_WeaponAndGear_Check()
		{
			var gear = SetupItem(2, GameId.MausHelmet, 1, 1);
			
			TestData.Inventory.Add(_item.Key, _item.Value);
			TestData.Inventory.Add(gear.Key, gear.Value);
			//TestData.InsertionTimestamps.Add(_item.Key, 0);
			//TestData.InsertionTimestamps.Add(gear.Key, 0);
			
			_equipmentLogic.Equip(_item.Key);
			_equipmentLogic.Equip(gear.Key);

			Assert.True(_equipmentLogic.GetInfo(_item.Key).IsEquipped);
			Assert.True(_playerData.Equipped.ContainsValue(_item.Key));
			Assert.True(_equipmentLogic.GetInfo(gear.Key).IsEquipped);
			Assert.True(_playerData.Equipped.ContainsValue(gear.Key));
		}
		
		[Test]
		public void Equip_SlotAlreadyEquipped_Replace()
		{
			var item = SetupItem(2, _item.Value.GameId);
			
			TestData.Inventory.Add(_item.Key, _item.Value);
			TestData.Inventory.Add(item.Key, item.Value);
			//TestData.InsertionTimestamps.Add(_item.Key, 0);
			//TestData.InsertionTimestamps.Add(item.Key, 0);
			
			_equipmentLogic.Equip(item.Key);
			_equipmentLogic.Equip(_item.Key);

			Assert.False(_equipmentLogic.GetInfo(item.Key).IsEquipped);
			Assert.True(_equipmentLogic.GetInfo(_item.Key).IsEquipped);
			Assert.False(_playerData.Equipped.ContainsValue(item.Key));
			Assert.True(_playerData.Equipped.ContainsValue(_item.Key));
		}
		
		[Test]
		public void Equip_NotInInventory_ThrowsException()
		{
			Assert.Throws<ArgumentException>(() => _equipmentLogic.Equip(UniqueId.Invalid));
		}
		
		[Test]
		public void UnequipCheck()
		{
			TestData.Inventory.Add(_item.Key, _item.Value);
			//TestData.InsertionTimestamps.Add(_item.Key, 0);
			_playerData.Equipped.Add(GameIdGroup.Weapon, _item.Key);
			
			_equipmentLogic.Unequip(_item.Key);

			Assert.False(_equipmentLogic.GetInfo(_item.Key).IsEquipped);
			Assert.False(_playerData.Equipped.ContainsValue(_item.Key));
		}
		
		[Test]
		public void Unequip_EmptySlot_ThrowsException()
		{
			TestData.Inventory.Add(_item.Key, _item.Value);
			//TestData.InsertionTimestamps.Add(_item.Key, 0);
			
			Assert.Throws<LogicException>(() => _equipmentLogic.Unequip(_item.Key));
		}
		
		[Test]
		public void Unequip_NotInInventory_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _equipmentLogic.Unequip(_item.Key));
		}

		private Pair<UniqueId, Equipment> SetupItem(UniqueId id, GameId gameId, uint level = 1, uint maxLevel = 1)
		{
			var item = new Equipment(gameId)
			{
				Level = level,
				MaxLevel = maxLevel
			};

			UniqueIdLogic.Ids[id].Returns(gameId);
			UniqueIdLogic.GenerateNewUniqueId(gameId).Returns(id);

			return new Pair<UniqueId, Equipment>(id, item);
		}
	}
}