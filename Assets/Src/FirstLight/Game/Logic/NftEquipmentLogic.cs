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
		
		public EquipmentInfo GetInfo(UniqueId id)
		{
			var cooldownMinutes = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>().NftUsageCooldownMinutes;
			var cooldownFinishTime = new DateTime(_insertionTimestamps[id]).AddMinutes(cooldownMinutes);
			var equipment = _inventory[id];
			
			if (!Data.ImageUrls.TryGetValue(id, out var url))
			{
				// TODO: Remove this once everything is working
				url = "https://flgmarketplacestorage.z33.web.core.windows.net/nftimages/0/1/0a7d0c215b6abbb3a0c4c9964b136f0f2ba36c1b4ba8fb797223415539af4e69.png";
			}

			return new EquipmentInfo
			{
				Id = id,
				Equipment = equipment,
				IsEquipped = _loadout.TryGetValue(equipment.GameId.GetSlot(), out var equipId) && equipId == id,
				NftCooldown = cooldownFinishTime - DateTime.UtcNow,
				CardUrl = url,
				Stats = GetEquipmentStats(equipment)
			};
		}

		public List<EquipmentInfo> GetLoadoutEquipmentInfo()
		{
			var ret = new List<EquipmentInfo>();

			foreach (var (slot, id) in _loadout)
			{
				ret.Add(GetInfo(id));
			}

			return ret;
		}

		public List<EquipmentInfo> GetInventoryEquipmentInfo()
		{
			var ret = new List<EquipmentInfo>();

			foreach (var (id, equipment) in _inventory)
			{
				var info = GetInfo(id);

				if (!info.IsOnCooldown)
				{
					ret.Add(info);
				}
			}

			return ret;
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
			return Loadout.Count >= GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>().NftRequiredEquippedForPlay;
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

		public void SetLoadout(IDictionary<GameIdGroup, UniqueId> newLoadout)
		{
			var slots = Equipment.EquipmentSlots;

			foreach (var slot in slots)
			{
				if (newLoadout.TryGetValue(slot, out var id))
				{
					if (_loadout.TryGetValue(slot, out var equippedId) && id != equippedId)
					{
						Equip(id);
					}
				}
				else if(_loadout.ContainsKey(slot))
				{
					Unequip(id);
				}
			}
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