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
		public IObservableDictionaryReader<UniqueId, long> _insertionTimestamps;

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

		/// <summary>
		/// Requests all equipped items in the loadout
		/// </summary>
		public Equipment[] GetLoadoutItems()
		{
			return _loadout.ReadOnlyDictionary.Values.Select(id => _inventory[id]).ToArray();
		}

		/// <summary>
		/// Requests all pieces of equipment for a given <paramref name="slot"/> from player's inventory
		/// </summary>
		public List<Equipment> FindInInventory(GameIdGroup slot)
		{
			return _inventory.ReadOnlyDictionary.Values.Where(equipment => equipment.GameId.IsInGroup(slot)).ToList();
		}

		/// <summary>
		/// Returns true if <paramref name="itemId"/> is equipped in loadout
		/// </summary>
		public bool IsEquipped(UniqueId itemId)
		{
			return _loadout.ReadOnlyDictionary.Values.Contains(itemId);
		}

		/// <summary>
		/// Requests the <paramref name="stat"/> for a given piece of equipment
		/// </summary>
		public float GetItemStat(Equipment equipment, StatType stat)
		{
			var gameConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var baseStatsConfig =
				GameLogic.ConfigsProvider.GetConfig<QuantumBaseEquipmentStatsConfig>((int) equipment.GameId);
			var statsConfig = GameLogic.ConfigsProvider.GetConfig<EquipmentStatsConfigs>().GetConfig(equipment);

			return QuantumStatCalculator.CalculateStat(gameConfig, baseStatsConfig, statsConfig, equipment, stat)
			                            .AsFloat;
		}

		/// <summary>
		/// Requests the total <paramref name="stat"/> for the equipped loadout
		/// </summary>
		public float GetTotalEquippedStat(StatType stat)
		{
			var value = 0f;

			foreach (var id in _loadout.ReadOnlyDictionary.Values)
			{
				value += GetItemStat(_inventory[id], stat);
			}

			return value;
		}

		/// <summary>
		/// Requests a dictionary of all stats for the given <paramref name="equipment"/> at level <paramref name="level"/>
		/// </summary>
		public Dictionary<EquipmentStatType, float> GetEquipmentStats(Equipment equipment, uint level = 0)
		{
			var stats = new Dictionary<EquipmentStatType, float>();
			var gameConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var baseStatsConfig =
				GameLogic.ConfigsProvider.GetConfig<QuantumBaseEquipmentStatsConfig>((int) equipment.GameId);
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

		/// <summary>
		/// Requests the remaining cooldown of a given <paramref name="itemId"/>
		/// </summary>
		public TimeSpan GetItemCooldown(UniqueId itemId)
		{
			double cooldownMinutes = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>().NftUsageCooldownMinutes;
			DateTime cooldownFinishTime = GetInsertionTime(itemId).AddMinutes(cooldownMinutes);
			
			return cooldownFinishTime - DateTime.UtcNow;
		}

		/// <summary>
		/// Requests the inventory insertion time of a given <paramref name="itemId"/>
		/// </summary>
		public DateTime GetInsertionTime(UniqueId itemId)
		{
			return new DateTime(_insertionTimestamps.ReadOnlyDictionary[itemId]);
		}

		// TODO: Remove method and refactor cheats
		/// <summary>
		/// Adds an item of ID <paramref name="equipment"/> to the inventory
		/// </summary>
		public UniqueId AddToInventory(Equipment equipment)
		{
			var id = GameLogic.UniqueIdLogic.GenerateNewUniqueId(equipment.GameId);
			_inventory.Add(id, equipment);
			return id;
		}
		
		// TODO: Remove method and refactor cheats
		/// <summary>
		/// Removes an item of ID <paramref name="equipment"/> from the inventory, and unequips it if necessary
		/// </summary>
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
			
			return true;
		}

		/// <summary>
		/// Equips an item of a given <paramref name="itemID"/>, that should be present in the inventory
		/// </summary>
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

		/// <summary>
		/// Unquips an item of a given <paramref name="itemID"/>, that should be present in the inventory
		/// </summary>
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