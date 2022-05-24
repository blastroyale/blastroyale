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

		public IObservableDictionaryReader<GameIdGroup, UniqueId> Loadout => _loadout;

		public IObservableDictionaryReader<UniqueId, Equipment> Inventory => _inventory;


		public NftEquipmentLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		public void Init()
		{
			_loadout =
				new ObservableDictionary<GameIdGroup, UniqueId>(DataProvider.GetData<PlayerData>().Equipped);
			_inventory = new ObservableDictionary<UniqueId, Equipment>(Data.Inventory);
		}

		public Equipment[] GetLoadoutItems()
		{
			return _loadout.ReadOnlyDictionary.Values.Select(id => _inventory[id]).ToArray();
		}

		public List<Equipment> FindInInventory(GameIdGroup slot)
		{
			return _inventory.ReadOnlyDictionary.Values.Where(equipment => equipment.GameId.IsInGroup(slot)).ToList();
		}

		public bool IsEquipped(UniqueId itemId)
		{
			return _loadout.ReadOnlyDictionary.Values.Contains(itemId);
		}

		public float GetItemStat(Equipment equipment, StatType stat)
		{
			var gameConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var statsConfig = GameLogic.ConfigsProvider.GetConfig<EquipmentStatsConfigs>().GetConfig(equipment);

			if (equipment.IsWeapon())
			{
				var weaponConfig = GameLogic.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) equipment.GameId);
				return QuantumStatCalculator.CalculateWeaponStat(gameConfig, weaponConfig, statsConfig,
				                                                 equipment, stat).AsFloat;
			}
			else
			{
				var gearConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGearConfig>((int) equipment.GameId);
				return QuantumStatCalculator.CalculateGearStat(gameConfig, gearConfig, statsConfig, equipment,
				                                               stat).AsFloat;
			}
		}

		public float GetTotalEquippedStat(StatType stat)
		{
			var value = 0f;

			foreach (var id in _loadout.ReadOnlyDictionary.Values)
			{
				value += GetItemStat(_inventory[id], stat);
			}

			return value;
		}

		public Dictionary<EquipmentStatType, float> GetEquipmentStats(Equipment equipment, uint level = 0)
		{
			var stats = new Dictionary<EquipmentStatType, float>();
			var gameConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var statsConfig = GameLogic.ConfigsProvider.GetConfig<EquipmentStatsConfigs>().GetConfig(equipment);

			if (level > 0)
			{
				equipment.Level = level;
			}

			if (equipment.GameId.IsInGroup(GameIdGroup.Weapon))
			{
				var weaponConfig = GameLogic.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) equipment.GameId);

				stats.Add(EquipmentStatType.SpecialId0, (float) weaponConfig.Specials[0]);
				stats.Add(EquipmentStatType.SpecialId1, (float) weaponConfig.Specials[1]);
				stats.Add(EquipmentStatType.MaxCapacity, weaponConfig.MaxAmmo);
				stats.Add(EquipmentStatType.TargetRange, weaponConfig.AttackRange.AsFloat);
				stats.Add(EquipmentStatType.AttackCooldown, weaponConfig.AttackCooldown.AsFloat);


				stats.Add(EquipmentStatType.Hp,
				          QuantumStatCalculator
					          .CalculateWeaponStat(gameConfig, weaponConfig, statsConfig, equipment, StatType.Health)
					          .AsFloat);
				stats.Add(EquipmentStatType.Speed,
				          QuantumStatCalculator
					          .CalculateWeaponStat(gameConfig, weaponConfig, statsConfig, equipment, StatType.Speed)
					          .AsFloat);
				stats.Add(EquipmentStatType.Armor,
				          QuantumStatCalculator
					          .CalculateWeaponStat(gameConfig, weaponConfig, statsConfig, equipment, StatType.Armour)
					          .AsFloat);
				stats.Add(EquipmentStatType.Damage,
				          QuantumStatCalculator
					          .CalculateWeaponStat(gameConfig, weaponConfig, statsConfig, equipment, StatType.Armour)
					          .AsFloat);
			}
			else
			{
				var gearConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGearConfig>((int) equipment.GameId);

				stats.Add(EquipmentStatType.Hp,
				          QuantumStatCalculator
					          .CalculateGearStat(gameConfig, gearConfig, statsConfig, equipment, StatType.Health)
					          .AsFloat);
				stats.Add(EquipmentStatType.Speed,
				          QuantumStatCalculator
					          .CalculateGearStat(gameConfig, gearConfig, statsConfig, equipment, StatType.Speed)
					          .AsFloat);
				stats.Add(EquipmentStatType.Armor,
				          QuantumStatCalculator
					          .CalculateGearStat(gameConfig, gearConfig, statsConfig, equipment, StatType.Armour)
					          .AsFloat);
				stats.Add(EquipmentStatType.Damage,
				          QuantumStatCalculator
					          .CalculateGearStat(gameConfig, gearConfig, statsConfig, equipment, StatType.Armour)
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

		public void Equip(UniqueId itemId)
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

		public void Unequip(UniqueId itemId)
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
	}
}