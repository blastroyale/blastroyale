using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <inheritdoc cref="IEquipmentLogic"/>
	public class NftEquipmentLogic : AbstractBaseLogic<NftEquipmentData>, IEquipmentLogic, IGameLogicInitializer
	{
		private IObservableDictionary<GameIdGroup, UniqueId> _loadout;
		private IObservableDictionary<UniqueId, Equipment> _inventory;
		private IObservableDictionary<UniqueId, long> _insertionTimestamps;
		
		public IObservableDictionaryReader<GameIdGroup, UniqueId> Loadout => _loadout;
		public IObservableDictionaryReader<UniqueId, Equipment> Inventory => _inventory;
		public IObservableDictionaryReader<UniqueId, long> InsertionTimestamps => _insertionTimestamps;

		public NftEquipmentLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		public void Init()
		{
			_loadout = new ObservableDictionary<GameIdGroup, UniqueId>(DataProvider.GetData<PlayerData>().Equipped);
			_inventory = new ObservableDictionary<UniqueId, Equipment>(Data.Inventory);
			_insertionTimestamps = new ObservableDictionary<UniqueId, long>(Data.InsertionTimestamps);
		}

		public Equipment[] GetLoadoutItems()
		{
			return _loadout.ReadOnlyDictionary.Values.Select(id => _inventory[id]).ToArray();
		}

		public Dictionary<UniqueId, Equipment> GetNftInventory()
		{
			var eligibleInventory = new Dictionary<UniqueId, Equipment>();
			
			foreach (var kvp in _inventory)
			{
				if (GameLogic.EquipmentLogic.GetItemCooldown(kvp.Key).TotalSeconds <= 0)
				{
					eligibleInventory.Add(kvp.Key,kvp.Value);
				}
			}

			return eligibleInventory;
		}

		public List<Equipment> FindInInventory(GameIdGroup slot)
		{
			return _inventory.ReadOnlyDictionary.Values.Where(equipment => equipment.GameId.IsInGroup(slot)).ToList();
		}

		public float GetItemStat(Equipment equipment, StatType stat)
		{
			var configsProvider = GameLogic.ConfigsProvider;
			var gameConfig = configsProvider.GetConfig<QuantumGameConfig>();
			var baseStatsConfig = configsProvider.GetConfig<QuantumBaseEquipmentStatsConfig>((int) equipment.GameId);
			var statsConfig = configsProvider.GetConfig<EquipmentStatsConfigs>().GetConfig(equipment);

			return QuantumStatCalculator.CalculateStat(gameConfig, baseStatsConfig, statsConfig, equipment, stat)
			                            .AsFloat;
		}

		public float GetTotalEquippedStat(StatType stat)
		{
			var value = 0f;

			foreach (var id in _loadout.ReadOnlyDictionary.Values)
			{
				if (id == UniqueId.Invalid)
				{
					throw new LogicException($"Item ID '{id}' not valid for stat calculations.");
					
				}
				value += GetItemStat(_inventory[id], stat);
			}

			return value;
		}

		public double GetDurabilityAveragePercentage(List<Equipment> items)
		{
			var currentNftDurabilities = 0d;
			var maxNftDurabilities = 0d;
			
			foreach (var nft in items)
			{
				currentNftDurabilities += nft.Durability;
				maxNftDurabilities += nft.MaxDurability;
			}

			return currentNftDurabilities / maxNftDurabilities;
		}

		public float GetTotalEquippedStat(StatType stat, List<UniqueId> items)
		{
			var value = 0f;
			
			foreach (var id in items)
			{
				if (id == UniqueId.Invalid)
				{
					throw new LogicException($"Item ID '{id}' not valid for stat calculations.");
				}
				
				value += GetItemStat(_inventory[id], stat);
			}

			return value;
		}

		public string GetEquipmentCardUrl(UniqueId id)
		{
			if (Data.ImageUrls.TryGetValue(id, out var url))
			{
				return url;
			}

			// TODO: Remove this once everything is working
			return "https://flgmarketplacestorage.z33.web.core.windows.net/nftimages/0/1/0a7d0c215b6abbb3a0c4c9964b136f0f2ba36c1b4ba8fb797223415539af4e69.png";
		}

		public Dictionary<EquipmentStatType, float> GetEquipmentStats(Equipment equipment)
		{
			var stats = new Dictionary<EquipmentStatType, float>();
			var gameConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var baseStatsConfig =
				GameLogic.ConfigsProvider.GetConfig<QuantumBaseEquipmentStatsConfig>((int) equipment.GameId);
			var statsConfig = GameLogic.ConfigsProvider.GetConfig<EquipmentStatsConfigs>().GetConfig(equipment);

			if (equipment.GameId.IsInGroup(GameIdGroup.Weapon))
			{
				var weaponConfig = GameLogic.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) equipment.GameId);

				stats.Add(EquipmentStatType.SpecialId0, (float) weaponConfig.Specials[0]);
				stats.Add(EquipmentStatType.SpecialId1, (float) weaponConfig.Specials[1]);
				stats.Add(EquipmentStatType.MaxCapacity, weaponConfig.MaxAmmo.Get(GameMode.BattleRoyale));
				stats.Add(EquipmentStatType.TargetRange, weaponConfig.AttackRange.AsFloat);
				stats.Add(EquipmentStatType.AttackCooldown, weaponConfig.AttackCooldown.AsFloat);
			}

			stats.Add(EquipmentStatType.Hp,
			          QuantumStatCalculator
				          .CalculateStat(gameConfig, baseStatsConfig, statsConfig, equipment, StatType.Health).AsFloat);
			stats.Add(EquipmentStatType.Speed,
			          QuantumStatCalculator
				          .CalculateStat(gameConfig, baseStatsConfig, statsConfig, equipment, StatType.Speed)
				          .AsFloat);
			stats.Add(EquipmentStatType.Armor,
			          QuantumStatCalculator
				          .CalculateStat(gameConfig, baseStatsConfig, statsConfig, equipment, StatType.Armour)
				          .AsFloat);
			stats.Add(EquipmentStatType.Damage,
			          QuantumStatCalculator
				          .CalculateStat(gameConfig, baseStatsConfig, statsConfig, equipment, StatType.Power)
				          .AsFloat);

			return stats;
		}

		public bool EnoughLoadoutEquippedToPlay()
		{
			var nftAmountForPlay = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>().NftRequiredEquippedForPlay;
			var nftCount = GetLoadoutItems().Length;
			
			return nftCount >= nftAmountForPlay;
		}

		public TimeSpan GetItemCooldown(UniqueId itemId)
		{
			var cooldownMinutes = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>().NftUsageCooldownMinutes;
			var cooldownFinishTime = GetInsertionTime(itemId).AddMinutes(cooldownMinutes);

			return cooldownFinishTime - DateTime.UtcNow;
		}

		// TODO: Remove method and refactor cheats
		public UniqueId AddToInventory(Equipment equipment)
		{
			var id = GameLogic.UniqueIdLogic.GenerateNewUniqueId(equipment.GameId);
			_inventory.Add(id, equipment);
			_insertionTimestamps.Add(id, DateTime.UtcNow.Ticks);
			return id;
		}

		// TODO: Remove method and refactor cheats
		public bool RemoveFromInventory(UniqueId equipment)
		{
			if (!_inventory.ContainsKey(equipment))
			{
				return false;
			}

			// Unequip the item before removing it from inventory
			var gameId = GameLogic.UniqueIdLogic.Ids[equipment];
			var slot = gameId.GetSlot();

			if (_loadout.TryGetValue(slot, out var equippedId))
			{
				Unequip(equippedId);
			}

			_inventory.Remove(equipment);
			_insertionTimestamps.Remove(equipment);
			GameLogic.UniqueIdLogic.RemoveId(equipment);
			
			return true;
		}

		public void SetLoadout(Dictionary<GameIdGroup, UniqueId> newLoadout)
		{
			foreach (var modifiedKvp in newLoadout)
			{
				UniqueId equippedInSlot = GetEquippedItemForSlot(modifiedKvp.Key);
				
				if (equippedInSlot != UniqueId.Invalid && modifiedKvp.Value == UniqueId.Invalid)
				{
					Unequip(equippedInSlot);
				}
				else if (modifiedKvp.Value != equippedInSlot)
				{
					Equip(modifiedKvp.Value);
				}
			}
		}
		
		public UniqueId GetEquippedItemForSlot(GameIdGroup idGroup)
		{
			if (!_loadout.ReadOnlyDictionary.ContainsKey(idGroup))
			{
				return UniqueId.Invalid;
			}
			
			return _loadout.ReadOnlyDictionary[idGroup];
		}

		private void Equip(UniqueId itemId)
		{
			var gameId = GameLogic.UniqueIdLogic.Ids[itemId];
			var slot = gameId.GetSlot();

			if (_loadout.TryGetValue(slot, out var equippedId))
			{
				if (equippedId == itemId)
				{
					throw new LogicException($"The player already has the given item Id '{itemId}' equipped");
				}

				Unequip(equippedId);
			}

			_loadout.Add(slot, itemId);
		}

		private void Unequip(UniqueId itemId)
		{
			var gameId = GameLogic.UniqueIdLogic.Ids[itemId];
			var slot = gameId.GetSlot();

			if (!_loadout.TryGetValue(slot, out var equippedId) || equippedId != itemId)
			{
				throw new
					LogicException($"The player does not have the given '{gameId}' item equipped to be unequipped");
			}

			_loadout.Remove(slot);
		}

		private DateTime GetInsertionTime(UniqueId itemId)
		{
			return new DateTime(_insertionTimestamps.ReadOnlyDictionary[itemId]);
		}
	}
}