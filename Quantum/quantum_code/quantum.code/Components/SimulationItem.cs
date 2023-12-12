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

		public static SimulationItem CreateEquipment(Equipment e)
		{
			var i = new SimulationItem();
			var equip = i.EquipmentItem;
			*equip = e;
			return i;
		}

		public static SimulationItem CreateSimple(GameId id)
		{
			var i = new SimulationItem();
			i.SimpleItem->Id = id;
			return i;
		}
	}
}