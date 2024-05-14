using System;

namespace Quantum
{
	public enum ItemType
	{
		Undefined, Equipment, Simple
	}
	public unsafe partial struct SimulationItem
	{
		public ItemType ItemType
		{
			get
			{
				switch (Field)
				{
					case SIMPLEITEM:    return ItemType.Simple;
					case EQUIPMENTITEM: return ItemType.Equipment;
				}
				return ItemType.Undefined;
			}
		}
		
		/// <summary>
		/// Creates an equipment type of item
		/// </summary>
		public static SimulationItem CreateEquipment(Equipment e)
		{
			var i = new SimulationItem();
			var equip = i.EquipmentItem;
			*equip = e;
			return i;
		}

		/// <summary>
		/// Creates an item that only consists of a game id
		/// </summary>
		public static SimulationItem CreateSimple(GameId id)
		{
			var i = new SimulationItem();
			i.SimpleItem->Id = id;
			return i;
		}

		public static SimulationItem FromConfig(in SimulationItemConfig config)
		{
			if (config.EquipmentMetadata.IsValid())
			{
				var item = CreateEquipment(config.EquipmentMetadata);
				if (item.EquipmentItem->Level == 0) item.EquipmentItem->Level = 1;
				return item;
			}
			if (config.SimpleGameId == GameId.Random)
			{
				throw new Exception("Invalid item config for simple id " + config.SimpleGameId);
			}  
			if (config.SimpleGameId.IsInGroup(GameIdGroup.Equipment))
			{
				return CreateEquipment(new Equipment(config.SimpleGameId));
			}
			return CreateSimple(config.SimpleGameId);
		}
	}
}