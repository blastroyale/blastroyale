using System.Collections.Generic;
using System.Linq;
using Quantum.Systems;

namespace Quantum
{
	public unsafe partial struct Chest
	{
		public void Open(Frame f, EntityRef e, EntityRef playerEntity, PlayerRef playerRef)
		{
			var noHealthNoShields = f.Context.TryGetMutatorByType(MutatorType.Hardcore, out _);
			var chestPosition = f.Unsafe.GetPointer<Transform3D>(e)->Position;
			var config = f.ChestConfigs.GetConfig(ChestType);
			List<SimulationItem> contents = null;
			if (f.TryGet<ChestContents>(e, out var chestContents))
			{
				contents = f.ResolveList(chestContents.Items).ToList();
			}
			if (contents == null || contents.Count == 0)
			{
				contents = ChestSystem.GenerateItems(f, e);
			}
			var step = 0;
			foreach (var drop in contents)
			{
				if (drop.ItemType == ItemType.Equipment)
				{
					Collectable.DropEquipment(f, *drop.EquipmentItem, chestPosition, step, true, contents.Count);
				} else if (drop.ItemType == ItemType.Simple)
				{
					if (noHealthNoShields &&
						(drop.SimpleItem->Id == GameId.Health ||
						 drop.SimpleItem->Id == GameId.ShieldSmall))
					{
						// Don't drop Health and Shields with Hardcore mutator
					}
					else
					{
						Collectable.DropConsumable(f, drop.SimpleItem->Id, chestPosition, step, true, contents.Count);
					}
				}
				step++;
			}
			f.Signals.ChestOpened(config.Id, chestPosition, playerRef, playerEntity);
			f.Events.OnChestOpened(config.Id, chestPosition, playerRef, playerEntity);
		}
	}
}