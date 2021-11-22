using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Infos
{
	public struct LootBoxInventoryInfo
	{
		public TimedBoxInfo?[] TimedBoxSlots;
		public List<TimedBoxInfo> TimedBoxExtra;
		public List<CoreBoxInfo> CoreBoxes;
		public uint SlotCount;
		public TimedBoxInfo? LootBoxUnlocking;
		public TimedBoxInfo? MainLootBox;

		/// <summary>
		/// Requests the unlocking cost of all extra loot boxes by the given <paramref name="time"/>
		/// </summary>
		public ulong GetUnlockExtraBoxesCost(DateTime time)
		{
			var cost = 0ul;
			
			foreach (var box in TimedBoxExtra)
			{
				cost += box.UnlockCost(time);
			}

			return cost;
		}

		/// <summary>
		/// Requests the count of slots filled with crates
		/// </summary>
		public int GetSlotsFilledCount()
		{
			var count = 0;

			foreach (var box in TimedBoxSlots)
			{
				if (box.HasValue)
				{
					count++;
				}
			}

			return count;
		}
	}

	public struct LootBoxInfo
	{
		public LootBoxConfig Config;
		public List<ItemRarity> PossibleRarities;
	}

	public struct CoreBoxInfo
	{
		public LootBoxData Data;
		public LootBoxConfig Config;
		public List<ItemRarity> PossibleRarities;
	}
	
	public struct TimedBoxInfo
	{
		public TimedBoxData Data;
		public LootBoxConfig Config;
		public List<ItemRarity> PossibleRarities;
		public float MinuteSpeedCost;

		/// <summary>
		/// Requests the <see cref="LootBoxState"/> at this given <paramref name="time"/>
		/// </summary>
		public LootBoxState GetState(DateTime time)
		{
			if (Config.IsAutoOpenCore || time > Data.EndTime && Data.UnlockingStarted)
			{
				return LootBoxState.Unlocked;
			}

			return Data.UnlockingStarted ? LootBoxState.Unlocking : LootBoxState.Locked;
		}

		/// <summary>
		/// Requests the unlocking cost of this loot by the given <paramref name="time"/>
		/// </summary>
		public ulong UnlockCost(DateTime time)
		{
			var state = GetState(time);
			
			if (state == LootBoxState.Unlocked)
			{
				return 0;
			}

			if (state == LootBoxState.Locked)
			{
				return Convert.ToUInt64(Math.Floor(Config.SecondsToOpen / 60f) * MinuteSpeedCost);
			}

			return time > Data.EndTime ? 0 : Convert.ToUInt64((Data.EndTime - time).TotalMinutes * MinuteSpeedCost);
		}
	}

	public enum LootBoxState
	{
		Locked,
		Unlocking,
		Unlocked,
	}
}