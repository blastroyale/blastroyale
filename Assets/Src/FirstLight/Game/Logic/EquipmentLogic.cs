using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic.RPC;
using FirstLight.Services;
using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.Logic
{
	public enum EquipmentFilter
	{
		Both,
		NftOnly,
		NoNftOnly
	}
	
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
		/// Requests the player's non NFT inventory.
		/// </summary>
		IObservableDictionaryReader<UniqueId, Equipment> Inventory { get; }

		/// <summary>
		/// Requests the player's non NFT inventory.
		/// </summary>
		IObservableDictionaryReader<UniqueId, NftEquipmentData> NftInventory { get; }

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for the given <paramref name="id"/>
		/// </summary>
		EquipmentInfo GetInfo(UniqueId id);

		/// <summary>
		/// Requests the <see cref="NftEquipmentInfo"/> for the given <paramref name="id"/>
		/// </summary>
		/// <exception cref="LogicException"> Thrown when the given <paramref name="id"/> is not of a NFT equipment type </exception>
		NftEquipmentInfo GetNftInfo(UniqueId id);

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for the given <paramref name="id"/>
		/// </summary>
		bool TryGetNftInfo(UniqueId id, out NftEquipmentInfo nftEquipmentInfo);

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for all the loadout with the given <paramref name="filter"/>
		/// </summary>
		List<EquipmentInfo> GetLoadoutEquipmentInfo(EquipmentFilter filter);

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for all the inventory with the given <paramref name="filter"/>
		/// </summary>
		List<EquipmentInfo> GetInventoryEquipmentInfo(EquipmentFilter filter);

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
		UniqueId AddToInventory(Equipment equipment);

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
	public class EquipmentLogic : AbstractBaseLogic<EquipmentData>, IEquipmentLogic, IGameLogicInitializer
	{
		private IObservableDictionary<GameIdGroup, UniqueId> _loadout;
		private IObservableDictionary<UniqueId, Equipment> _inventory;
		private IObservableDictionary<UniqueId, NftEquipmentData> _nftInventory;
		public IObservableDictionaryReader<GameIdGroup, UniqueId> Loadout => _loadout;
		public IObservableDictionaryReader<UniqueId, Equipment> Inventory => _inventory;
		public IObservableDictionaryReader<UniqueId, NftEquipmentData> NftInventory => _nftInventory;

		public EquipmentLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		public void Init()
		{
			_loadout = new ObservableDictionary<GameIdGroup, UniqueId>(DataProvider.GetData<PlayerData>().Equipped);
			_inventory = new ObservableDictionary<UniqueId, Equipment>(Data.Inventory);
			_nftInventory = new ObservableDictionary<UniqueId, NftEquipmentData>(Data.NftInventory);
		}
		
		public EquipmentInfo GetInfo(UniqueId id)
		{
			var equipment = _inventory[id];
			
			return new EquipmentInfo
			{
				Id = id,
				Equipment = equipment,
				IsNft = _nftInventory.ContainsKey(id),
				IsEquipped = _loadout.TryGetValue(equipment.GameId.GetSlot(), out var equipId) && equipId == id,
				Stats = GetEquipmentStats(equipment)
			};
		}

		public NftEquipmentInfo GetNftInfo(UniqueId id)
		{
			if (!TryGetNftInfo(id, out var info))
			{
				throw new LogicException($"The given '{id}' id is not an equipment of NFT type");
			}

			return info;
		}

		public bool TryGetNftInfo(UniqueId id, out NftEquipmentInfo nftEquipmentInfo)
		{
			if (!_nftInventory.TryGetValue(id, out var nftData))
			{
				nftEquipmentInfo = default;
				
				return false;
			}

			nftEquipmentInfo = new NftEquipmentInfo
			{
				EquipmentInfo = GetInfo(id),
				NftData = nftData,
				NftCooldownInMinutes = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>().NftUsageCooldownMinutes,
			};
			
			return true;
		}

		public List<EquipmentInfo> GetLoadoutEquipmentInfo(EquipmentFilter filter)
		{
			var ret = new List<EquipmentInfo>();

			foreach (var (slot, id) in _loadout)
			{
				var contains = Data.NftInventory.ContainsKey(id);
				
				if (filter == EquipmentFilter.NftOnly && !contains || filter == EquipmentFilter.NoNftOnly && contains)
				{
					continue;
				}

				ret.Add(GetInfo(id));
			}

			return ret;
		}

		public List<EquipmentInfo> GetInventoryEquipmentInfo(EquipmentFilter filter)
		{
			var ret = new List<EquipmentInfo>();

			foreach (var (id, equipment) in _inventory)
			{
				var contains = Data.NftInventory.ContainsKey(id);
				
				if (filter == EquipmentFilter.NftOnly && !contains || filter == EquipmentFilter.NoNftOnly && contains)
				{
					continue;
				}
				
				ret.Add(GetInfo(id));
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
			var statsMaterialConfig = GameLogic.ConfigsProvider.GetConfig<QuantumEquipmentMaterialStatsConfig>(equipment.GetMaterialStatsKey());

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
				          .CalculateStat(gameConfig, baseStatsConfig, statsConfig, statsMaterialConfig, equipment, StatType.Health).AsFloat);
			stats.Add(EquipmentStatType.Speed,
			          QuantumStatCalculator
				          .CalculateStat(gameConfig, baseStatsConfig, statsConfig, statsMaterialConfig, equipment, StatType.Speed)
				          .AsFloat);
			stats.Add(EquipmentStatType.Armor,
			          QuantumStatCalculator
				          .CalculateStat(gameConfig, baseStatsConfig, statsConfig, statsMaterialConfig, equipment, StatType.Armour)
				          .AsFloat);
			stats.Add(EquipmentStatType.Damage,
			          QuantumStatCalculator
				          .CalculateStat(gameConfig, baseStatsConfig, statsConfig, statsMaterialConfig, equipment, StatType.Power)
				          .AsFloat);

			return stats;
		}

		public bool EnoughLoadoutEquippedToPlay()
		{
			return Loadout.Count >= GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>().NftRequiredEquippedForPlay;
		}

		public UniqueId AddToInventory(Equipment equipment)
		{
			if (!equipment.GameId.IsInGroup(GameIdGroup.Equipment))
			{
				throw new LogicException($"The given '{equipment.GameId}' id is not of '{GameIdGroup.Equipment}'" +
				                         "game id group");
			}
			
			var id = GameLogic.UniqueIdLogic.GenerateNewUniqueId(equipment.GameId);
			
			_inventory.Add(id, equipment);
			
			return id;
		}

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
			
			GameLogic.UniqueIdLogic.RemoveId(equipment);
			
			return true;
		}

		public void SetLoadout(IDictionary<GameIdGroup, UniqueId> newLoadout)
		{
			var slots = Equipment.EquipmentSlots;

			foreach (var slot in slots)
			{
				var isEquippingSlot = newLoadout.TryGetValue(slot, out var equipAddToSlot);
				if (isEquippingSlot)
				{
					if (!_loadout.TryGetValue(slot, out var equippedId) || equipAddToSlot != equippedId)
					{
						Equip(equipAddToSlot);
					}
				}
				else if(_loadout.ContainsKey(slot))
				{
					Unequip(_loadout[slot]);
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