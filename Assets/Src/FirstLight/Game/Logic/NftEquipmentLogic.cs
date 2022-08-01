using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic.RPC;
using FirstLight.Services;
using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's equipment
	/// </summary>
	public interface IEquipmentDataProvider
	{
		/// <summary>
		/// Requests the player's loadout.
		/// </summary>
		IObservableDictionaryReader<GameIdGroup, UniqueId> Loadout { get; }

		/// <summary>
		/// Requests the player's inventory.
		/// </summary>
		IObservableDictionaryReader<UniqueId, Equipment> Inventory { get; }

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for the given <paramref name="id"/>
		/// </summary>
		EquipmentInfo GetInfo(UniqueId id);

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for all the loadout
		/// </summary>
		List<EquipmentInfo> GetLoadoutEquipmentInfo();

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for all the inventory
		/// </summary>
		List<EquipmentInfo> GetInventoryEquipmentInfo();

		/// <summary>
		/// Request the stats a specific piece of equipment has
		/// </summary>
		Dictionary<EquipmentStatType, float> GetEquipmentStats(Equipment equipment);

		/// <summary>
		/// Requests to see if player has enough NFTs equipped for play
		/// </summary>
		bool EnoughLoadoutEquippedToPlay();
	}

	/// <inheritdoc />
	public interface IEquipmentLogic : IEquipmentDataProvider
	{
		/// <summary>
		/// Adds an item to the inventory and assigns it a new UniqueId.
		/// </summary>
		UniqueId AddToInventory(Equipment equipment, long overrideTimestamp = -1);

		/// <summary>
		/// Tries to remove an item from inventory, and returns true if a removal was successful
		/// </summary>
		bool RemoveFromInventory(UniqueId equipment);

		/// <summary>
		/// Sets the loadout for each slot in given <paramref name="newLoadout"/>
		/// </summary>
		void SetLoadout(IDictionary<GameIdGroup, UniqueId> newLoadout);
		
		/// <summary>
		/// Equips the given <paramref name="itemId"/> to the player's Equipment slot.
		/// </summary>
		void Equip(UniqueId itemId);

		/// <summary>
		/// Unequips the given <paramref name="itemId"/> from the player's Equipment slot.
		/// </summary>
		void Unequip(UniqueId itemId);

	}
	
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
			_loadout = new ObservableDictionary<GameIdGroup, UniqueId>(DataProvider.GetData<PlayerData>().Equipped);
			_inventory = new ObservableDictionary<UniqueId, Equipment>(Data.Inventory);
		}
		
		public EquipmentInfo GetInfo(UniqueId id)
		{
			var cooldownMinutes = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>().NftUsageCooldownMinutes;
			var cooldownFinishTime = new DateTime(Data.InsertionTimestamps[id]).AddMinutes(cooldownMinutes);
			var equipment = _inventory[id];
			
			if (!Data.ImageUrls.TryGetValue(id, out var url))
			{
				// TODO: Remove this once everything is working
				url = "https://flgmarketplacestorage.z33.web.core.windows.net/nftimages/0/1/0a7d0c215b6abbb3a0c4c9964b136f0f2ba36c1b4ba8fb797223415539af4e69.png";
			}

			// Because old jsons didn't had SSL, making it backwards compatible
			// we need SSL for iOS because <random Apple rant>
			url = url.Replace("http:", "https:");
			
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
			var statsConfig = GameLogic.ConfigsProvider.GetConfig<QuantumEquipmentStatsConfig>(equipment.GetStatsKey());

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
		public UniqueId AddToInventory(Equipment equipment, long overrideTimestamp = -1)
		{
			if (!equipment.GameId.IsInGroup(GameIdGroup.Equipment))
			{
				throw new LogicException($"The given '{equipment.GameId}' id is not of '{GameIdGroup.Equipment}'" +
				                         "game id group");
			}
			
			var id = GameLogic.UniqueIdLogic.GenerateNewUniqueId(equipment.GameId);
			
			_inventory.Add(id, equipment);
			
			if (overrideTimestamp >= 0)
			{
				Data.InsertionTimestamps.Add(id, overrideTimestamp);
			}
			else
			{
				Data.InsertionTimestamps.Add(id, DateTime.UtcNow.Ticks);
			}
			
			return id;
		}

		// TODO: Remove method and refactor cheats
		public bool RemoveFromInventory(UniqueId equipment)
		{
			if (!_inventory.ContainsKey(equipment))
			{
				throw new LogicException($"The given '{equipment}' id is not in the inventory");
			}

			// Unequip the item before removing it from inventory
			var gameId = GameLogic.UniqueIdLogic.Ids[equipment];
			var slot = gameId.GetSlot();

			if (_loadout.TryGetValue(slot, out var equippedId))
			{
				Unequip(equippedId);
			}

			_inventory.Remove(equipment);
			Data.InsertionTimestamps.Remove(equipment);
			Data.TokenIds.Remove(equipment);
			Data.ImageUrls.Remove(equipment);
			Data.ExpireTimestamps.Remove(equipment);
			Data.LastUpdateTimestamp = 0;
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
					if (!_loadout.TryGetValue(slot, out var equippedId) || id != equippedId)
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