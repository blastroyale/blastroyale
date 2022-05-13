using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Services;
using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <inheritdoc cref="IEquipmentLogic"/>
	public class EquipmentLogic : AbstractBaseLogic<PlayerData>, IEquipmentLogic, IGameLogicInitializer
	{
		private IObservableDictionary<GameIdGroup, UniqueId> _equippedItems;
		private IObservableDictionary<UniqueId, Equipment> _inventory;

		public IObservableDictionaryReader<GameIdGroup, UniqueId> EquippedItems => _equippedItems;

		public IObservableDictionaryReader<UniqueId, Equipment> Inventory => _inventory;

		public EquipmentLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			_equippedItems = new ObservableDictionary<GameIdGroup, UniqueId>(Data.EquippedItems);
			_inventory = new ObservableDictionary<UniqueId, Equipment>(Data.Inventory);
		}

		public Equipment GetEquippedWeapon()
		{
			// TODO: Does this work with the Hammer?
			return _inventory[_equippedItems[GameIdGroup.Weapon]];
		}

		public List<Equipment> GetEquippedGear()
		{
			var gear = new List<Equipment>();

			foreach (var (group, id) in _equippedItems.ReadOnlyDictionary)
			{
				if (group != GameIdGroup.Weapon)
				{
					gear.Add(_inventory[id]);
				}
			}

			return gear;
		}

		public List<Equipment> FindInInventory(GameIdGroup slot)
		{
			throw new NotImplementedException();
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

		public UniqueId AddToInventory(Equipment equipment)
		{
			// TODO: Is this ok?
			var id = GameLogic.UniqueIdLogic.GenerateNewUniqueId(equipment.GameId);
			_inventory.Add(id, equipment);
			return id;
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

		/// <inheritdoc />
		public void Upgrade(UniqueId itemId)
		{
			var equipment = _inventory[itemId];

			if (equipment.Level == equipment.MaxLevel)
			{
				throw new
					LogicException($"The given item with the id {itemId} already reached the max level of {equipment.MaxLevel}");
			}

			equipment.Level++;

			_inventory[itemId] = equipment;
		}

		public uint GetUpgradeCost(Equipment equipment)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<RarityConfig>();

			var upgradeCost =
				Math.Round(config.UpgradeBasePrice * Math.Pow(config.UpgradePriceLevelPowerOf, equipment.Level));

			return upgradeCost < 1000
				       ? (uint) Math.Floor(upgradeCost / 10) * 10
				       : (uint) Math.Floor(upgradeCost / 100) * 100;
		}
	}
}