using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	public struct ChestItemDropped
	{
		public GameId ChestType;
		public FPVector3 ChestPosition;
		public PlayerRef Player;
		public EntityRef PlayerEntity;
		public GameId ItemType;
		public int Amount;
		public int AngleStepAroundChest;
	}
	
	public unsafe partial class EventOnChestOpened
	{
		public List<ChestItemDropped> Items;
	}
	
	public partial class Frame
	{
		public unsafe partial struct FrameEvents
		{
			public void OnChestOpened(GameId ChestType, FPVector3 ChestPosition, PlayerRef Player, EntityRef Entity, List<ChestItemDropped> Items)
			{
				var chestOpenedEvent = OnChestOpened(ChestType, ChestPosition, Player, Entity);
				if (chestOpenedEvent == null)
				{
					return;
				}
				chestOpenedEvent.Items = Items;
			}
		}
	}
}