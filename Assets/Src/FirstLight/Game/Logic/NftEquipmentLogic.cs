using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Services;
using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <inheritdoc cref="IEquipmentLogic"/>
	public class NftEquipmentLogic : AbstractBaseLogic<NftEquipmentData>, IEquipmentLogic, IGameLogicInitializer
	{
		private IObservableDictionary<GameIdGroup, UniqueId> _equippedItems;
		private IObservableDictionary<UniqueId, Equipment> _inventory;

		public IObservableDictionaryReader<GameIdGroup, UniqueId> Loadout => _equippedItems;

		public IObservableDictionaryReader<UniqueId, Equipment> Inventory => _inventory;

		
		
		public NftEquipmentLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			_equippedItems = new ObservableDictionary<GameIdGroup, UniqueId>(DataProvider.GetData<PlayerData>().Equipped);
			_inventory = new ObservableDictionary<UniqueId, Equipment>(Data.Inventory);
		}

		public Equipment[] GetLoadoutItems()
		{
			return _equippedItems.ReadOnlyDictionary.Values.Select(id => _inventory[id]).ToArray();
		}

		public List<Equipment> FindInInventory(GameIdGroup slot)
		{
			return _inventory.ReadOnlyDictionary.Values.Where(equipment => equipment.GameId.IsInGroup(slot)).ToList();
		}

		/// <inheritdoc />
		public bool IsEquipped(UniqueId itemId)
		{
			return _equippedItems.ReadOnlyDictionary.Values.Contains(itemId);
		}


		public uint GetItemPower(Equipment equipment)
		{
			var gameConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();

			return (uint) equipment.Rarity * gameConfig.EquipmentRarityToPowerK +
			       equipment.Level * gameConfig.EquipmentLevelToPowerK;
		}

		/// <inheritdoc />
		public uint GetTotalEquippedItemPower()
		{
			var power = 0u;

			foreach (var id in _equippedItems.ReadOnlyDictionary.Values)
			{
				power += GetItemPower(_inventory[id]);
			}

			return power;
		}

		public Dictionary<EquipmentStatType, float> GetEquipmentStats(Equipment equipment, uint level = 0)
		{
			var stats = new Dictionary<EquipmentStatType, float>();
			var gameConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();

			if (level > 0)
			{
				equipment.Level = level;
			}

			if (equipment.GameId.IsInGroup(GameIdGroup.Weapon))
			{
				var weaponConfig = GameLogic.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) equipment.GameId);
				var power = QuantumStatCalculator.CalculateWeaponPower(gameConfig, weaponConfig, equipment);

				stats.Add(EquipmentStatType.SpecialId0, (float) weaponConfig.Specials[0]);
				stats.Add(EquipmentStatType.SpecialId1, (float) weaponConfig.Specials[1]);
				stats.Add(EquipmentStatType.MaxCapacity, weaponConfig.MaxAmmo);
				stats.Add(EquipmentStatType.TargetRange, weaponConfig.AttackRange.AsFloat);
				stats.Add(EquipmentStatType.AttackCooldown, weaponConfig.AttackCooldown.AsFloat);
				stats.Add(EquipmentStatType.Damage, power.AsFloat);
			}
			else
			{
				var gearConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGearConfig>((int) equipment.GameId);

				stats.Add(EquipmentStatType.Hp,
				          QuantumStatCalculator.CalculateGearStat(gameConfig, gearConfig, equipment, StatType.Health)
				                               .AsFloat);
				stats.Add(EquipmentStatType.Speed,
				          QuantumStatCalculator.CalculateGearStat(gameConfig, gearConfig, equipment, StatType.Speed)
				                               .AsFloat);
				stats.Add(EquipmentStatType.Armor,
				          QuantumStatCalculator.CalculateGearStat(gameConfig, gearConfig, equipment, StatType.Armour)
				                               .AsFloat);
			}

			return stats;
		}

		// TODO: Remove method and refactor cheats
		public UniqueId AddToInventory(Equipment equipment)
		{
			var id = GameLogic.UniqueIdLogic.GenerateNewUniqueId(equipment.GameId);
			_inventory.Add(id, equipment);
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

			if (_equippedItems.TryGetValue(slot, out var equippedId))
			{
				Unequip(equippedId);
			}
			
			_inventory.Remove(equipment);
			
			return true;
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

				Unequip(equippedId);
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
				throw new
					LogicException($"The player does not have the given '{gameId}' item equipped to be unequipped");
			}

			_equippedItems.Remove(slot);
		}
	}
}