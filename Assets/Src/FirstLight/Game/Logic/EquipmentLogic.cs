using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Services;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <inheritdoc cref="IEquipmentLogic"/>
	public class EquipmentLogic : AbstractBaseLogic<PlayerData>, IEquipmentLogic, IGameLogicInitializer
	{
		private IObservableDictionary<GameIdGroup, UniqueId> _equippedItems;
		private IObservableList<EquipmentData> _inventory;
		
		/// <inheritdoc />
		public IObservableDictionaryReader<GameIdGroup, UniqueId> EquippedItems => _equippedItems;
		/// <inheritdoc />
		public IObservableListReader<EquipmentData> Inventory => _inventory;

		public EquipmentLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			_equippedItems = new ObservableDictionary<GameIdGroup, UniqueId>(Data.EquippedItems);
			_inventory = new ObservableList<EquipmentData>(Data.Inventory);
		}

		/// <inheritdoc />
		public EquipmentDataInfo GetEquipmentDataInfo(UniqueId itemId)
		{
			var index = GetItemIndex(itemId);
			var gameId = GameLogic.UniqueIdDataProvider.Ids[itemId];
			
			if (index < 0)
			{
				throw new LogicException($"The player does not have the given item Id '{itemId}' of type " +
				                         $"{gameId} in it's inventory.");
			}

			return new EquipmentDataInfo
			{
				Data = _inventory[index],
				GameId = gameId
			};
		}

		/// <inheritdoc />
		public EquipmentInfo GetEquipmentInfo(UniqueId itemId)
		{
			var data = GetEquipmentDataInfo(itemId);
			var info = GetEquipmentInfo(data.GameId, data.Data.Rarity, data.Data.Level);

			info.DataInfo.Data.Id = itemId;
			info.IsInInventory = true;
			info.IsEquipped = IsEquipped(itemId);

			return info;
		}

		/// <inheritdoc />
		public EquipmentInfo GetEquipmentInfo(GameId gameId)
		{
			var rarity = ItemRarity.Common;
			
			if (!gameId.IsInGroup(GameIdGroup.Weapon))
			{
				var gearConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGearConfig>((int) gameId);

				rarity = gearConfig.StartingRarity;
			}

			return GetEquipmentInfo(gameId, rarity, 1);
		}

		/// <inheritdoc />
		public EquipmentInfo GetEquipmentInfo(GameId gameId, ItemRarity rarity, uint level)
		{
			if (!gameId.IsInGroup(GameIdGroup.Equipment))
			{
				throw new LogicException($"The item {gameId} is not a {GameIdGroup.Equipment} type");
			}
			
			var gameConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var rarityConfig = GameLogic.ConfigsProvider.GetConfig<RarityConfig>((int) rarity);
			var data = new EquipmentDataInfo(gameId, rarity, level);
			var stats = new Dictionary<EquipmentStatType, float>(new EquipmentStatTypeComparer());
			var baseRarity = rarity;
			var isWeapon = false;

			if (gameId.IsInGroup(GameIdGroup.Weapon))
			{
				var weaponConfig = GameLogic.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) gameId);
				var damage = QuantumStatCalculator.CalculateStatValue(rarity, weaponConfig.PowerRatioToBase, level, gameConfig, StatType.Power);

				baseRarity = ItemRarity.Common;
				isWeapon = true;
				
				stats.Add(EquipmentStatType.SpecialId0, (float) weaponConfig.Specials[0]);
				stats.Add(EquipmentStatType.SpecialId1, (float) weaponConfig.Specials[1]);
				stats.Add(EquipmentStatType.MaxCapacity, weaponConfig.MaxAmmo);
				stats.Add(EquipmentStatType.ProjectileSpeed, weaponConfig.ProjectileSpeed.AsFloat);
				stats.Add(EquipmentStatType.TargetRange, weaponConfig.AttackRange.AsFloat);
				stats.Add(EquipmentStatType.AttackCooldown, weaponConfig.AttackCooldown.AsFloat);
				stats.Add(EquipmentStatType.Damage, damage.AsFloat);
			}
			else
			{
				var gearConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGearConfig>((int) gameId);
				var hp = QuantumStatCalculator.CalculateStatValue(rarity, gearConfig.HpRatioToBase, level, gameConfig, StatType.Health);
				var speed = QuantumStatCalculator.CalculateStatValue(rarity, gearConfig.SpeedRatioToBase, level, gameConfig, StatType.Speed);
				var armor = QuantumStatCalculator.CalculateStatValue(rarity, gearConfig.ArmorRatioToBase, level, gameConfig, StatType.Armour);
				
				stats.Add(EquipmentStatType.Hp, hp.AsFloat);
				stats.Add(EquipmentStatType.Speed, speed.AsFloat);
				stats.Add(EquipmentStatType.Armor, armor.AsFloat);
			}

			if ((int) rarity < (int) baseRarity)
			{
				throw new LogicException($"The minimum rarity for the item {gameId} is {baseRarity} and not {rarity}");
			}
			
			return new EquipmentInfo
			{
				DataInfo = data,
				Stats = stats,
				BaseRarity = baseRarity,
				ItemPower = GetItemPower(rarity, level),
				IsEquipped = false,
				IsInInventory = false,
				IsWeapon = isWeapon,
				MaxLevel = rarityConfig.MaxLevel,
				SellCost = GetSellCost(rarity, level),
				UpgradeCost = GetUpgradeCost(rarityConfig, level)
			};
		}

		/// <inheritdoc />
		public List<EquipmentDataInfo> GetEquipmentDataInfoList(ItemRarity rarity)
		{
			var intRarity = (int) rarity;
			var loot = new List<EquipmentDataInfo>();
			// var weaponConfigs = GameLogic.ConfigsProvider.GetConfigsList<QuantumWeaponConfig>();
			var gearConfigs = GameLogic.ConfigsProvider.GetConfigsList<QuantumGearConfig>();
			
			// foreach (var weapon in weaponConfigs)
			// {
			// 	if ((int) weapon.StartingRarity > intRarity)
			// 	{
			// 		continue;
			// 	}
			// 			
			// 	loot.Add(new EquipmentDataInfo(weapon.Id, rarity, 1));
			// }
			
			foreach (var gear in gearConfigs)
			{
				if ((int) gear.StartingRarity > intRarity)
				{
					continue;
				}
				
				loot.Add(new EquipmentDataInfo(gear.Id, rarity, 1));
			}
			
			return loot;
		}

		/// <inheritdoc />
		public WeaponInfo GetWeaponInfo(UniqueId itemId)
		{
			if (!TryGetWeaponInfo(itemId, out var info))
			{
				throw new LogicException($"The item {GameLogic.UniqueIdLogic.Ids[itemId]} is not a {GameIdGroup.Weapon} type");
			}
			
			return info;
		}

		/// <inheritdoc />
		public WeaponInfo GetWeaponInfo(GameId gameId)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) gameId);

			return GetWeaponInfo(gameId, ItemRarity.Common, 1);
		}

		/// <inheritdoc />
		public WeaponInfo GetWeaponInfo(GameId gameId, ItemRarity rarity, uint level)
		{
			if (!gameId.IsInGroup(GameIdGroup.Weapon))
			{
				throw new LogicException($"The item {gameId} is not a {GameIdGroup.Weapon} type");
			}

			return new WeaponInfo
			{
				EquipmentInfo = GetEquipmentInfo(gameId, rarity, level),
				WeaponConfig = GameLogic.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) gameId)
			};
		}

		/// <inheritdoc />
		public bool TryGetWeaponInfo(UniqueId itemId, out WeaponInfo info)
		{
			var gameId = GameLogic.UniqueIdLogic.Ids[itemId];
			
			if (!gameId.IsInGroup(GameIdGroup.Weapon) || !gameId.IsInGroup(GameIdGroup.Equipment))
			{
				info = default;
				
				return false;
			}
			
			info = new WeaponInfo
			{
				EquipmentInfo = GetEquipmentInfo(itemId),
				WeaponConfig = GameLogic.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) gameId)
			};

			return true;
		}

		/// <inheritdoc />
		public GearInfo GetGearInfo(UniqueId itemId)
		{
			if (!TryGetGearInfo(itemId, out var info))
			{
				throw new LogicException($"The item {GameLogic.UniqueIdLogic.Ids[itemId]} is not a gear equipment type");
			}
			
			return info;
		}

		/// <inheritdoc />
		public GearInfo GetGearInfo(GameId gameId)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<QuantumGearConfig>((int) gameId);

			return GetGearInfo(gameId, config.StartingRarity, 1);
		}

		/// <inheritdoc />
		public GearInfo GetGearInfo(GameId gameId, ItemRarity rarity, uint level)
		{
			if (gameId.IsInGroup(GameIdGroup.Weapon))
			{
				throw new LogicException($"The item {gameId} is not a gear equipment type");
			}

			return new GearInfo
			{
				EquipmentInfo = GetEquipmentInfo(gameId, rarity, level),
				GearConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGearConfig>((int) gameId)
			};
		}

		/// <inheritdoc />
		public bool TryGetGearInfo(UniqueId itemId, out GearInfo info)
		{
			var gameId = GameLogic.UniqueIdLogic.Ids[itemId];
			
			if (gameId.IsInGroup(GameIdGroup.Weapon) || !gameId.IsInGroup(GameIdGroup.Equipment))
			{
				info = default;
				
				return false;
			}
			
			info = new GearInfo
			{
				EquipmentInfo = GetEquipmentInfo(itemId),
				GearConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGearConfig>((int) gameId)
			};

			return true;
		}

		/// <inheritdoc />
		public List<EquipmentDataInfo> GetInventoryInfo(GameIdGroup slot)
		{
			var list = new List<EquipmentDataInfo>();
			
			for (var i = 0; i < _inventory.Count; i++)
			{
				var data = _inventory[i];
				var gameId = GameLogic.UniqueIdLogic.Ids[(int) data.Id.Id];

				if (!gameId.IsInGroup(slot))
				{
					continue;
				}
				
				list.Add(new EquipmentDataInfo { Data = data, GameId = gameId });
			}

			return list;
		}

		/// <inheritdoc />
		public EquipmentLoadOutInfo GetLoadOutInfo()
		{
			var ret = new EquipmentLoadOutInfo
			{
				Weapon = null,
				Gear = new List<EquipmentDataInfo>()
			};

			foreach (var equippedItem in Data.EquippedItems)
			{
				var info = GetEquipmentDataInfo(equippedItem.Value);

				ret.TotalItemPower += GetItemPower(info.Data.Rarity, info.Data.Level);
				
				if (equippedItem.Key == GameIdGroup.Weapon)
				{
					ret.Weapon = info;
				}
				else
				{
					ret.Gear.Add(info);
				}
			}
				
			return ret;
		}

		/// <inheritdoc />
		public FusionInfo GetFusionInfo(List<UniqueId> items)
		{
			if (items.Count == 0)
			{
				throw new LogicException("The fusion items list is empty. It needs at leas 1 item to calculate " +
				                         $"the necessary {nameof(FusionInfo)}");
			}
			
			var gameConfigs = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var rarity = ItemRarity.TOTAL;
			var resultPercentage = new Dictionary<GameIdGroup, uint>();
			var fusingItems = new List<EquipmentDataInfo>();
			var totalWeight = 0u;

			foreach (var slot in Constants.EquipmentSlots)
			{
				totalWeight += gameConfigs.FusionBaseWeightPerType;
				
				resultPercentage.Add(slot, gameConfigs.FusionBaseWeightPerType);
			}

			foreach (var itemId in items)
			{
				var info = GetEquipmentDataInfo(itemId);
				var weight = gameConfigs.FusionWeightIncreasePerItem + gameConfigs.FusionWeightIncreasePerLevel * info.Data.Level;

				if (rarity != ItemRarity.TOTAL && info.Data.Rarity != rarity)
				{
					throw new LogicException($"The item {info.GameId} does not have the same fusion base rarity of {rarity}");
				}

				rarity = info.Data.Rarity;
				totalWeight += weight;
				resultPercentage[info.GameId.GetSlot()] += weight;
				
				fusingItems.Add(info);
			}

			foreach (var slot in Constants.EquipmentSlots)
			{
				if (resultPercentage.TryGetValue(slot, out var weight))
				{
					resultPercentage[slot] = (uint) (weight * 100f / totalWeight);
				}
			}
			
			return new FusionInfo
			{
				FusingItems = fusingItems,
				ResultPercentages = resultPercentage,
				FusingRarity = rarity,
				FusingResultRarity = rarity == ItemRarity.Legendary ? rarity : (ItemRarity) ((int) rarity + 1),
				FusingCost = GameLogic.ConfigsProvider.GetConfig<RarityConfig>((int) rarity).FusionCost
			};
		}

		/// <inheritdoc />
		public EnhancementInfo GetEnhancementInfo(List<UniqueId> items)
		{
			if (items.Count == 0)
			{
				throw new LogicException($"The item list to enhance is empty. It needs at leas 1 item to calculate " +
				                         $"the necessary {nameof(EnhancementInfo)}");
			}
			
			var rarity = ItemRarity.TOTAL;
			var enhancementItems = new List<EquipmentDataInfo>();
			var result = new EquipmentDataInfo(GameId.Random, ItemRarity.TOTAL, 1);
			var upgradeCost = 0u;
			var rarityConfig = new RarityConfig();
			var hasEquipped = false;
			
			foreach (var itemId in items)
			{
				var info = GetEquipmentDataInfo(itemId);

				if (rarity == ItemRarity.TOTAL)
				{
					rarity = info.Data.Rarity;
					result.GameId = info.GameId;
					result.Data.Rarity = rarity == ItemRarity.Legendary ? rarity : (ItemRarity) ((int) rarity + 1);
					rarityConfig = GameLogic.ConfigsProvider.GetConfig<RarityConfig>((int) rarity);
				}

				if (info.Data.Rarity != rarity || info.GameId != result.GameId)
				{
					throw new LogicException($"The item {info.GameId} does not have the same enhancement properties" +
					                         $"of other items with {result.GameId} & {rarity} properties");
				}
				
				for (uint i = 1; i < info.Data.Level; i++)
				{
					upgradeCost += GetUpgradeCost(rarityConfig, i);
				}

				hasEquipped |= _equippedItems.TryGetValue(info.GameId.GetSlot(), out var equipped) &&
				               GameLogic.UniqueIdDataProvider.Ids[equipped] == info.GameId;
				
				enhancementItems.Add(info);
			}

			var resultRarityConfig = GameLogic.ConfigsProvider.GetConfig<RarityConfig>((int) result.Data.Rarity);

			// Process the result level
			for (var cost = GetUpgradeCost(resultRarityConfig, result.Data.Level); 
			     result.Data.Level < resultRarityConfig.MaxLevel && cost < upgradeCost; 
			     result.Data.Level++)
			{
				cost += GetUpgradeCost(resultRarityConfig, result.Data.Level);
			}
			
			return new EnhancementInfo
			{
				EnhancementItems = enhancementItems,
				EnhancementResult = result,
				EnhancementCost = rarityConfig.EnhancementCost,
				EnhancementItemRequiredAmount = rarityConfig.EnhancementItemAmount,
				HasEquippedItem = hasEquipped
			};
		}

		/// <inheritdoc />
		public bool IsEquipped(UniqueId itemId)
		{
			foreach (var pair in _equippedItems)
			{
				if (pair.Value == itemId)
				{
					return true;
				}
			}

			return false;
		}

		/// <inheritdoc />
		public uint GetUpgradeCost(ItemRarity rarity, uint level)
		{
			return GetUpgradeCost(GameLogic.ConfigsProvider.GetConfig<RarityConfig>((int) rarity), level);
		}

		/// <inheritdoc />
		public uint GetSellCost(ItemRarity rarity, uint level)
		{
			var rarityConfig = GameLogic.ConfigsProvider.GetConfig<RarityConfig>((int) rarity);
			var sellCost = Math.Round(rarityConfig.SellBasePrice * Math.Pow(rarityConfig.SellPriceLevelPowerOf, level));
			
			return sellCost < 1000 ? (uint) Math.Floor(sellCost / 10) * 10 : (uint) Math.Floor(sellCost / 100) * 100;
		}

		/// <inheritdoc />
		public uint GetItemPower(ItemRarity rarity, uint level)
		{
			var gameConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			
			return (uint) rarity * gameConfig.EquipmentRarityToPowerK + level * gameConfig.EquipmentLevelToPowerK;
		}

		/// <inheritdoc />
		public uint GetTotalEquippedItemPower()
		{
			var power = 0u;
			
			foreach (var item in _equippedItems)
			{
				var info = GetEquipmentDataInfo(item.Value);
				
				power += GetItemPower(info.Data.Rarity, info.Data.Level);
			}
			
			return power;
		}

		/// <inheritdoc />
		public EquipmentDataInfo AddToInventory(GameId item, ItemRarity rarity, uint level)
		{
			var data = new EquipmentData
			{
				Id = GameLogic.UniqueIdLogic.GenerateNewUniqueId(item),
				Rarity = rarity,
				Level = level
			};
			
			_inventory.Add(data);

			return new EquipmentDataInfo { Data = data, GameId = item };
		}

		/// <inheritdoc />
		public void Equip(UniqueId itemId)
		{
			var gameId = GameLogic.UniqueIdLogic.Ids[itemId];
			var slot = gameId.GetSlot();

			if (_equippedItems.TryGetValue(slot, out var equippedId))
			{
				if (equippedId == itemId)
				{
					throw new LogicException($"The player already has the given item Id '{itemId}' equipped");
				}
				
				_equippedItems.Remove(slot);
			}
			
			_equippedItems.Add(slot, itemId);
		}

		/// <inheritdoc />
		public void Unequip(UniqueId itemId)
		{
			var gameId = GameLogic.UniqueIdLogic.Ids[itemId];
			var slot = gameId.GetSlot();
			
			if (!_equippedItems.TryGetValue(slot, out var equippedId) || equippedId != itemId)
			{
				throw new LogicException($"The player does not have the given '{gameId}' item equipped to be unequipped");
			}
			
			_equippedItems.Remove(slot);
		}
		
		/// <inheritdoc />
		public void Sell(UniqueId itemId)
		{
			RemoveItem(itemId);
		}

		/// <inheritdoc />
		public void Upgrade(UniqueId itemId)
		{
			var info = GetEquipmentInfo(itemId);

			if (info.DataInfo.Data.Level == info.MaxLevel)
			{
				throw new LogicException($"The given item with the id {itemId} already reached the max level of {info.MaxLevel}");
			}
			
			var index = GetItemIndex(itemId);
			var data = _inventory[index];

			data.Level++;

			_inventory[index] = data;
		}

		/// <inheritdoc />
		public EquipmentDataInfo Fuse(List<UniqueId> items)
		{
			if(items.Count != GameConstants.FUSION_SLOT_AMOUNT)
			{
				throw new LogicException($"Wrong amount fusion items {items.Count}. Needed {GameConstants.FUSION_SLOT_AMOUNT}");
			}
			
			var info = GetFusionInfo(items);
			var resultList = info.ResultPercentages.ToList();
			var totalWeight = 0u;
			var equipmentList = GetEquipmentDataInfoList(info.FusingResultRarity);

			foreach (var item in items)
			{
				RemoveItem(item);
			}

			foreach (var pair in resultList)
			{
				totalWeight += pair.Value;
			}

			var rng = GameLogic.RngLogic.Range(0, (int) totalWeight);
			
			totalWeight = 0u;

			foreach (var pair in resultList)
			{
				totalWeight += pair.Value;

				if (rng >= totalWeight)
				{
					continue;
				}

				var possibleItems = new List<EquipmentDataInfo>(equipmentList);
				var possibleItemIds = pair.Key.GetIds();

				possibleItems.RemoveAll(dataInfo => !possibleItemIds.Contains(dataInfo.GameId));
				
				var item = possibleItems[GameLogic.RngLogic.Range(0, possibleItems.Count)];

				return AddToInventory(item.GameId, info.FusingResultRarity, 1);
			}

			throw new LogicException($"Error occurred while generating the fusing item with {info.FusingRarity} " +
			                         $"rarity and total weight of {totalWeight}");
		}

		/// <inheritdoc />
		public EquipmentDataInfo Enhance(List<UniqueId> items)
		{
			var info = GetEnhancementInfo(items);
			
			if(items.Count != info.EnhancementItemRequiredAmount)
			{
				throw new LogicException($"Wrong amount enhancement items {items.Count}. Needed {info.EnhancementItemRequiredAmount}");
			}

			foreach (var item in items)
			{
				RemoveItem(item);
			}

			var newItem = AddToInventory(info.EnhancementResult.GameId, info.EnhancementResult.Data.Rarity, info.EnhancementResult.Data.Level);

			if (info.HasEquippedItem)
			{
				Equip(newItem.Data.Id);
			}
			
			return newItem;
		}

		private int GetItemIndex(UniqueId itemId)
		{
			for (var i = 0; i < _inventory.Count; i++)
			{
				if (_inventory[i].Id == itemId)
				{
					return i;
				}
			}

			return -1;
		}

		private void RemoveItem(UniqueId item)
		{
			var index = GetItemIndex(item);
			var slot = GameLogic.UniqueIdLogic.Ids[item].GetSlot();

			if (_equippedItems.TryGetValue(slot, out var itemId) && itemId == item)
			{
				_equippedItems.Remove(slot);
			}

			_inventory.RemoveAt(index);
			GameLogic.UniqueIdLogic.RemoveId(item);
		}
		
		private uint GetUpgradeCost(RarityConfig config, uint level)
		{
			var upgradeCost = Math.Round(config.UpgradeBasePrice * Math.Pow(config.UpgradePriceLevelPowerOf, level));

			return upgradeCost < 1000 ? (uint) Math.Floor(upgradeCost / 10) * 10 : (uint) Math.Floor(upgradeCost / 100) * 100;
		}
	}
}