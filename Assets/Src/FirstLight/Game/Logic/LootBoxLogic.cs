using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's possible loot box behaviour & outcome
	/// </summary>
	public interface ILootBoxDataProvider
	{
		/// <summary>
		/// Requests the <see cref="LootBoxInventoryInfo"/> of all loot boxes in the player inventory
		/// </summary>
		LootBoxInventoryInfo GetLootBoxInventoryInfo();

		/// <summary>
		/// Requests the <see cref="LootBoxInfo"/> of the given <paramref name="lootBoxId"/>
		/// </summary>
		LootBoxInfo GetLootBoxInfo(int lootBoxId);

		/// <summary>
		/// Requests the <see cref="LootBoxInfo"/> of the given <paramref name="id"/>
		/// </summary>
		LootBoxInfo GetLootBoxInfo(UniqueId id);

		/// <summary>
		/// Requests the <see cref="TimedBoxInfo"/> of the given <paramref name="id"/>
		/// </summary>
		TimedBoxInfo GetTimedBoxInfo(UniqueId id);

		/// <summary>
		/// Requests the <see cref="TimedBoxInfo"/> of the given <paramref name="id"/>
		/// </summary>
		CoreBoxInfo GetCoreBoxInfo(UniqueId id);
		
		/// <summary>
		/// Peeks inside a the loot box of the given <paramref name="id"/> and returns the 
		/// </summary>
		List<EquipmentDataInfo> Peek(UniqueId id);
	}

	/// <inheritdoc />
	public interface ILootBoxLogic : ILootBoxDataProvider
	{
		/// <summary>
		/// Starts the unlocking timer for the loot box of the given <paramref name="id"/>
		/// </summary>
		void StartUnlocking(UniqueId id);
		
		/// <summary>
		/// Opens the loot box of the given <paramref name="id"/> and returns the items content in the box.
		/// The returned items are not yet added to the inventory or the system
		/// </summary>
		List<EquipmentDataInfo> Open(UniqueId id);
		
		/// <summary>
		/// Speeds up the loot box defined by the given <paramref name="id"/>
		/// </summary>
		void SpeedUp(UniqueId id);

		/// <summary>
		/// Speeds Ups all extra timed boxes that are not being unlocked
		/// </summary>
		void SpeedUpAllExtraTimedBoxes();

		/// <summary>
		/// Removes all timed boxes that don't fit on the slots
		/// </summary>
		void CleanExtraTimedBoxes();

		/// <summary>
		/// Adds a new loot box defined by the given <paramref name="lootBucketId"/> to the inventory.
		/// </summary>
		UniqueId AddToInventory(int lootBucketId);
	}

	/// <inheritdoc cref="ILootBoxLogic"/>
	public class LootBoxLogic : AbstractBaseLogic<PlayerData>, ILootBoxLogic
	{
		public LootBoxLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public LootBoxInventoryInfo GetLootBoxInventoryInfo()
		{
			var data = Data;
			var timeNow = GameLogic.TimeService.DateTimeUtcNow;
			var slotCount = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>().LootboxSlotsMaxNumber;
			var info = new LootBoxInventoryInfo
			{
				TimedBoxSlots = new TimedBoxInfo?[(int) slotCount],
				TimedBoxExtra = new List<TimedBoxInfo>(),
				CoreBoxes = new List<CoreBoxInfo>(),
				SlotCount = slotCount,
				LootBoxUnlocking = null,
				MainLootBox = null
			};

			foreach (var coreBox in data.CoreBoxes)
			{
				info.CoreBoxes.Add(GetCoreBoxInfo(coreBox.Id));
			}

			foreach (var timedBox in data.TimedBoxes)
			{
				var boxInfo = GetTimedBoxInfo(timedBox.Id);
				var boxState = boxInfo.GetState(timeNow);
				
				if (boxInfo.Data.Slot >= info.SlotCount)
				{
					info.TimedBoxExtra.Add(boxInfo);
					continue;
				}

				if (boxState == LootBoxState.Unlocked)
				{
					info.MainLootBox = boxInfo;
				}
				else if (boxState == LootBoxState.Unlocking)
				{
					info.LootBoxUnlocking = boxInfo;
				}
				
				info.TimedBoxSlots[boxInfo.Data.Slot] = boxInfo;
			}

			if (info.LootBoxUnlocking.HasValue)
			{
				info.MainLootBox = info.LootBoxUnlocking;
			}

			return info;
		}

		/// <inheritdoc />
		public LootBoxInfo GetLootBoxInfo(int lootBoxId)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<LootBoxConfig>(lootBoxId);

			return new LootBoxInfo
			{
				Config = config,
				PossibleRarities = GetPossibleRarities(config)
			};
		}

		/// <inheritdoc />
		public LootBoxInfo GetLootBoxInfo(UniqueId id)
		{
			if (TryGetTimedBoxData(id, out _, out var timedBoxData))
			{
				return GetLootBoxInfo(timedBoxData.ConfigId);
			}
			
			if (TryGetCoreBoxData(id, out _, out var coreBoxData))
			{
				return GetLootBoxInfo(coreBoxData.ConfigId);
			}
			
			throw new LogicException($"There is no Loot Box defined by the given {id} id");
		}

		/// <inheritdoc />
		public TimedBoxInfo GetTimedBoxInfo(UniqueId id)
		{
			if (!TryGetTimedBoxData(id, out _, out var timedBoxData))
			{
				throw new LogicException($"There is no Timed Box defined by the given {id} id");
			}
			
			var speedCost = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>().MinuteCostInHardCurrency.AsFloat;
			var config = GameLogic.ConfigsProvider.GetConfig<LootBoxConfig>(timedBoxData.ConfigId);

			return new TimedBoxInfo
			{
				Data = timedBoxData,
				Config = config,
				PossibleRarities = GetPossibleRarities(config),
				MinuteSpeedCost = speedCost
			};
		}

		/// <inheritdoc />
		public CoreBoxInfo GetCoreBoxInfo(UniqueId id)
		{
			if (!TryGetCoreBoxData(id, out _, out var coreBoxData))
			{
				throw new LogicException($"There is no Core Box defined by the given {id} id");
			}
			
			var config = GameLogic.ConfigsProvider.GetConfig<LootBoxConfig>(coreBoxData.ConfigId);

			return new CoreBoxInfo
			{
				Data = coreBoxData,
				Config = config,
				PossibleRarities = GetPossibleRarities(config)
			};
		}

		/// <inheritdoc />
		public List<EquipmentDataInfo> Peek(UniqueId id)
		{
			var counter = GameLogic.RngLogic.Counter;
			var configId = -1;

			if (TryGetTimedBoxData(id, out _, out var timedBoxData))
			{
				configId = timedBoxData.ConfigId;
			}

			if (configId < 0 && TryGetCoreBoxData(id, out _, out var coreBoxData))
			{
				configId = coreBoxData.ConfigId;
			}

			if (configId < 0)
			{
				throw new LogicException($"There is no Loot Box defined by the given {id} in the player's inventory");
			}
			
			var result = GetLoot(configId);
			
			GameLogic.RngLogic.Restore(counter);
			
			return result;
		}

		/// <inheritdoc />
		public void StartUnlocking(UniqueId id)
		{
			var inventoryInfo = GetLootBoxInventoryInfo();

			if (inventoryInfo.LootBoxUnlocking.HasValue)
			{
				throw new LogicException("There is already a box being unlocked. Speed it up first before starting a new one");
			}
			
			if (!TryGetTimedBoxData(id, out var index, out var timedBoxData))
			{
				throw new LogicException($"There is no Timed Box defined by the given {id} id");
			}
			
			var config = GameLogic.ConfigsProvider.GetConfig<LootBoxConfig>(timedBoxData.ConfigId);
			
			timedBoxData.UnlockingStarted = true;
			timedBoxData.EndTime = GameLogic.TimeService.DateTimeUtcNow.AddSeconds(config.SecondsToOpen);
			Data.TimedBoxes[index] = timedBoxData;

		}

		/// <inheritdoc />
		public List<EquipmentDataInfo> Open(UniqueId id)
		{
			var time = GameLogic.TimeService.DateTimeUtcNow;
			var configId = -1;

			if (TryGetTimedBoxData(id, out var timedIndex, out var timedBoxData))
			{
				if (time < timedBoxData.EndTime)
				{
					throw new LogicException($"The Loot Box defined by the given {id} is still unlocking and not ready to be open");
				}

				configId = timedBoxData.ConfigId;
				
				Data.TimedBoxes.RemoveAt(timedIndex);
			}

			if (configId < 0 && TryGetCoreBoxData(id, out var coreIndex, out var coreBoxData))
			{
				configId = coreBoxData.ConfigId;
				
				Data.CoreBoxes.RemoveAt(coreIndex);
			}

			if (configId < 0)
			{
				throw new LogicException($"There is no Loot Box defined by the given {id} in the player's inventory");
			}

			GameLogic.UniqueIdLogic.RemoveId(id);

			return GetLoot(configId);
		}

		/// <inheritdoc />
		public void SpeedUp(UniqueId id)
		{
			if (!TryGetTimedBoxData(id, out var index, out var timedBoxData))
			{
				throw new LogicException($"There is no Timed Box defined by the given {id} id");
			}

			timedBoxData.UnlockingStarted = true;
			timedBoxData.EndTime = GameLogic.TimeService.DateTimeUtcNow;
			Data.TimedBoxes[index] = timedBoxData;
		}

		/// <inheritdoc />
		public void SpeedUpAllExtraTimedBoxes()
		{
			var slotCount = (int) GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>().LootboxSlotsMaxNumber;

			if (slotCount >= Data.TimedBoxes.Count)
			{
				throw new LogicException("There are no extra loot boxes to be speed up");
			}
			
			for (var i = slotCount; i < Data.TimedBoxes.Count; i++)
			{
				SpeedUp(Data.TimedBoxes[i].Id);
			}
		}

		/// <inheritdoc />
		public void CleanExtraTimedBoxes()
		{
			var slotCount = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>().LootboxSlotsMaxNumber;

			if (slotCount >= Data.TimedBoxes.Count)
			{
				throw new LogicException("There are no extra loot boxes to be cleaned up");
			}
			
			for (var i = Data.TimedBoxes.Count - 1; i >= slotCount; i--)
			{
				GameLogic.UniqueIdLogic.RemoveId(Data.TimedBoxes[i].Id);
				Data.TimedBoxes.RemoveAt(i);
			}
		}

		/// <inheritdoc />
		public UniqueId AddToInventory(int lootBoxId)
		{
			var data = Data;
			var config = GameLogic.ConfigsProvider.GetConfig<LootBoxConfig>(lootBoxId);
			var uniqueID = GameLogic.UniqueIdLogic.GenerateNewUniqueId(config.LootBoxId);
			var lootBoxData = new LootBoxData(uniqueID, lootBoxId);

			if (config.IsAutoOpenCore)
			{
				data.CoreBoxes.Add(lootBoxData);

				return uniqueID;
			}
			
			var box = new TimedBoxData
			{
				Id = uniqueID,
				ConfigId = lootBoxId,
				EndTime = DateTime.MinValue,
				UnlockingStarted = false,
				Slot = -1
			};
			
			for (var i = 0; i < data.TimedBoxes.Count; i++)
			{
				if (data.TimedBoxes[i].Slot != i)
				{
					box.Slot = i;
				
					data.TimedBoxes.Insert(i, box);
					break;
				}
			}

			if (box.Slot == -1)
			{
				box.Slot = data.TimedBoxes.Count;
				
				data.TimedBoxes.Add(box);
			}

			return uniqueID;
		}

		private bool TryGetTimedBoxData(UniqueId id, out int index, out TimedBoxData data)
		{
			var boxes = Data.TimedBoxes;
			
			index = boxes.FindIndex(box => box.Id == id);
			data = index >= 0 ? boxes[index] : new TimedBoxData();

			return index >= 0;
		}

		private bool TryGetCoreBoxData(UniqueId id, out int index, out LootBoxData data)
		{
			var boxes = Data.CoreBoxes;
			
			index = boxes.FindIndex(box => box.Id == id);
			data = index >= 0 ? boxes[index] : new LootBoxData();

			return index >= 0;
		}

		private List<EquipmentDataInfo> GetLoot(int lootBoxId)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<LootBoxConfig>(lootBoxId);
			var loot = new List<EquipmentDataInfo>((int) config.ItemsAmount);

			// Check for fixed items first
			if (config.FixedItems.Count > 0)
			{
				var fixedItems = new List<Pair<GameId, ItemRarity>>(config.FixedItems);
				
				for (var i = 0; i < config.ItemsAmount; i++)
				{
					var index = GameLogic.RngLogic.Range(0, fixedItems.Count);
					
					loot.Add(new EquipmentDataInfo(fixedItems[index].Key, fixedItems[index].Value, ItemAdjective.Cool, ItemMaterial.Bronze, ItemManufacturer.Military, ItemFaction.Order, 1, 5));
					fixedItems.RemoveAt(index);
				}
					
				return loot;
			}
			
			var lootCache = new Dictionary<ItemRarity, List<EquipmentDataInfo>>();
			var rarities = new List<ItemRarity>((int) config.ItemsAmount);

			rarities.AddRange(config.GuaranteeDrop);

			for (var i = rarities.Count; i < config.ItemsAmount; i++)
			{
				rarities.Add(GetRandomRarity(config));
			}

			foreach (var rarity in rarities)
			{
				if (!lootCache.TryGetValue(rarity, out var equipmentList))
				{
					equipmentList = GameLogic.EquipmentDataProvider.GetEquipmentDataInfoList(rarity);
					
					lootCache.Add(rarity, equipmentList);
				}
				
				loot.Add(equipmentList[GameLogic.RngLogic.Range(0, equipmentList.Count)]);
			}

			return loot;
		}

		private ItemRarity GetRandomRarity(LootBoxConfig config)
		{
			var weightSum = 0u;

			foreach (var rarity in config.Rarities)
			{
				weightSum += rarity.Key;
			}
				
			var weight = GameLogic.RngLogic.Range(0, (int) weightSum);

			weightSum = 0;
			
			foreach (var rarity in config.Rarities)
			{
				weightSum += rarity.Key;
				
				if (weight < weightSum)
				{
					return rarity.Value;
				}
			}

			throw new ArgumentOutOfRangeException($"Cannot get valid rarity for loot box {config.Id}");
		}

		private List<ItemRarity> GetPossibleRarities(LootBoxConfig config)
		{
			var possibleRarities = new List<ItemRarity>();
			
			foreach (var item in config.FixedItems)
			{
				if (!possibleRarities.Contains(item.Value))
				{
					possibleRarities.Add(item.Value);
				}
			}

			if (possibleRarities.Count == 0)
			{
				foreach (var rarity in config.Rarities)
				{
					possibleRarities.Add(rarity.Value);
				}
			}

			return possibleRarities;
		}
	}
}