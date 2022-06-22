using System.Collections.Generic;
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
	public class EquipmentLogicTest : BaseTestFixture<PlayerData>
	{
		private Equipment _item;
		private NftEquipmentLogic _equipmentLogic;

		[SetUp]
		public void Init()
		{
			_item = SetupItem(1, GameId.Hammer, 1, 2);
			_equipmentLogic = new NftEquipmentLogic(GameLogic, DataService);
			
			_equipmentLogic.Init();
		}
		
		[Test]
		public void AddToInventoryCheck()
		{
			var id = _equipmentLogic.AddToInventory(_item);

			Assert.AreEqual(1, _equipmentLogic.Inventory.Count);
			Assert.AreSame(_item, _equipmentLogic.Inventory[id]);
		}
		
		[Test]
		public void AddToInventory_AlreadyInInventory_ThrowsException()
		{
			_equipmentLogic.AddToInventory(_item);
			
			Assert.Throws<LogicException>(() => _equipmentLogic.AddToInventory(_item));
		}
		
		[Test]
		public void AddToInventory_NotEquipment_ThrowsException()
		{
			var item = SetupItem(1, GameId.Barrel, 1, 2);
			
			Assert.Throws<LogicException>(() => _equipmentLogic.AddToInventory(item));
		}
		
		[Test]
		public void RemoveFromInventoryCheck()
		{
			var id = _equipmentLogic.AddToInventory(_item);

			Assert.True(_equipmentLogic.RemoveFromInventory(id));
			Assert.AreEqual(0, _equipmentLogic.Inventory.Count);
		}
		
		[Test]
		public void SetLoadoutCheck()
		{
			var id = _equipmentLogic.AddToInventory(_item);
			var group = _item.GameId.GetGroups()[0];
			var dic = new Dictionary<GameIdGroup, UniqueId> { { group, id } };
			
			_equipmentLogic.SetLoadout(dic);

			Assert.True(_equipmentLogic.RemoveFromInventory(id));
			Assert.AreEqual(1, _equipmentLogic.Loadout.Count);
			Assert.AreEqual(id, _equipmentLogic.Loadout[group]);
		}
		
		[Test]
		public void SetLoadoutCheck_DoubleCall_ReplaceSlot()
		{
			var item = SetupItem(2, GameId.ApoCrossbow, 1, 2);
			var id1 = _equipmentLogic.AddToInventory(_item);
			var id2 = _equipmentLogic.AddToInventory(item);
			var group = _item.GameId.GetGroups()[0];
			_equipmentLogic.SetLoadout(new Dictionary<GameIdGroup, UniqueId> { { group, id1 } });
			_equipmentLogic.SetLoadout(new Dictionary<GameIdGroup, UniqueId> { { group, id2 } });

			Assert.AreEqual(1, _equipmentLogic.Loadout.Count);
			Assert.AreNotEqual(id1, _equipmentLogic.Loadout[group]);
			Assert.AreEqual(id2, _equipmentLogic.Loadout[group]);
		}
		
		[Test]
		public void SetLoadout_RemoveFromInventory_UnequipItem()
		{
			var id = _equipmentLogic.AddToInventory(_item);
			var dic = new Dictionary<GameIdGroup, UniqueId> { { _item.GameId.GetGroups()[0], id } };
			
			_equipmentLogic.SetLoadout(dic);

			Assert.True(_equipmentLogic.RemoveFromInventory(id));
			Assert.AreEqual(0, _equipmentLogic.Inventory.Count);
		}
		
		[Test]
		public void RemoveFromInventory_NotInventory_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _equipmentLogic.RemoveFromInventory(UniqueId.Invalid));
		}

		private Equipment SetupItem(UniqueId id, GameId gameId, uint level = 1, uint maxLevel = 1)
		{
			var item = new Equipment(gameId)
			{
				Level = level,
				MaxLevel = maxLevel
			};

			UniqueIdLogic.Ids[id].Returns(gameId);
			UniqueIdLogic.GenerateNewUniqueId(gameId).Returns(id);

			return item;
		}
	}
}