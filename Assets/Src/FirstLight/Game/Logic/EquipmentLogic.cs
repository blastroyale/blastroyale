using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic.RPC;
using FirstLight.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Logic
{
	public enum EquipmentFilter
	{
		All,
		NftOnly,
		NoNftOnly,
		Broken,
		Unbroken
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
		/// Requests the given's <paramref name="equipment"/> scrapping reward
		/// </summary>
		Pair<GameId, uint> GetScrappingReward(Equipment equipment, bool isNft);

		/// <summary>
		/// Requests the given's <paramref name="equipment"/> upgrade cost for 1 level
		/// </summary>
		Pair<GameId, uint> GetUpgradeCost(Equipment equipment, bool isNft);

		/// <summary>
		/// Requests the given's <paramref name="equipment"/> repair cost
		/// </summary>
		Pair<GameId, uint> GetRepairCost(Equipment equipment, bool isNft);

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for the given <paramref name="id"/> of an
		/// inventory item.
		/// </summary>
		EquipmentInfo GetInfo(UniqueId id);

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for the given <paramref name="equipment"/>
		/// </summary>
		EquipmentInfo GetInfo(Equipment equipment, bool isNft = false);

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
		/// Requests to see if player has enough NFTs equipped for play
		/// </summary>
		bool EnoughLoadoutEquippedToPlay();

		/// <summary>
		/// Generates a new unique non-NFT piece of equipment from battle pass reward configs
		/// </summary>
		Equipment GenerateEquipmentFromConfig(EquipmentRewardConfig config);
	}

	/// <inheritdoc />
	public interface IEquipmentLogic : IEquipmentDataProvider
	{
		/// <summary>
		/// Adds an item to the inventory and assigns it a new UniqueId.
		/// </summary>
		UniqueId AddToInventory(Equipment equipment);

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

		/// <summary>
		/// Scraps the equipment of the given <paramref name="itemId"/> and returns the reward of scrapping the item
		/// </summary>
		Pair<GameId, uint> Scrap(UniqueId itemId);

		/// <summary>
		/// Upgrades the equipment of the given <paramref name="itemId"/> by one level
		/// </summary>
		void Upgrade(UniqueId itemId);

		/// <summary>
		/// Repairs the equipment of the given <paramref name="itemId"/> to full durability
		/// </summary>
		void Repair(UniqueId itemId);

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

		public Pair<GameId, uint> GetScrappingReward(Equipment equipment, bool isNft)
		{
			var resourceType = isNft ? GameId.CS : GameId.COIN;
			var config = GameLogic.ConfigsProvider.GetConfig<ScrapConfig>((int) resourceType);
			var rarityValue =
				Math.Ceiling(config.BaseValue * Math.Pow(config.GrowthMultiplier.AsDouble, (int)equipment.Rarity));
			var adjectiveValue = Math.Sqrt(Math.Pow(config.AdjectiveCostK.AsDouble, (int)equipment.Adjective));
			var gradeValue = Math.Pow(config.GradeMultiplier.AsDouble, (int)equipment.Grade);
			var levelMultiplier = ((int) equipment.Level * config.LevelMultiplier).AsDouble;
			var winValue = rarityValue + adjectiveValue + ((rarityValue + adjectiveValue) * levelMultiplier * gradeValue);

			return new Pair<GameId, uint>(resourceType, (uint) Math.Round(winValue));
		}

		public Pair<GameId, uint> GetUpgradeCost(Equipment equipment, bool isNft)
		{
			var resourceType = isNft ? GameId.CS : GameId.COIN;
			var config = GameLogic.ConfigsProvider.GetConfig<UpgradeDataConfig>((int) resourceType);
			var rarityValue = Math.Ceiling(config.BaseValue *
			                               Math.Pow(config.GrowthMultiplier.AsDouble, (int)equipment.Rarity));
			var adjectiveValue = Math.Sqrt(Math.Pow(config.AdjectiveCostK.AsDouble, (int)equipment.Adjective)) -
			                     config.AdjectiveCostScale;
			var gradeValue = Math.Pow(config.GradeMultiplier.AsDouble, (int)equipment.Grade);
			var levelMultiplier = ((int)equipment.Level * config.LevelMultiplier).AsDouble;
			var cost = rarityValue + adjectiveValue + 
			          ((rarityValue + adjectiveValue) * (int) equipment.Level * 
				          levelMultiplier * gradeValue * equipment.MaxDurability / config.DurabilityDivider);

			return new Pair<GameId, uint>(resourceType, (uint) Math.Round(cost));
		}

		public Pair<GameId, uint> GetRepairCost(Equipment equipment, bool isNft)
		{
			var resourceType = isNft ? GameId.CS : GameId.COIN;
			var config = GameLogic.ConfigsProvider.GetConfig<RepairDataConfig>((int) resourceType);
			var durability =
				equipment.GetCurrentDurability(isNft, GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>(),
				                               GameLogic.TimeService.DateTimeUtcNow.Ticks);
			var restoredAmount = (double) (equipment.MaxDurability - durability);
			var cost =
				Math.Pow((int) equipment.TotalRestoredDurability * config.DurabilityCostIncreasePerPoint.AsDouble * restoredAmount,
				         config.BasePower.AsDouble) * (int) config.BaseRepairCost;

			return new Pair<GameId, uint>(resourceType, (uint) Math.Round(cost));
		}

		public EquipmentInfo GetInfo(UniqueId id)
		{
			var info = GetInfo(_inventory[id], _nftInventory.ContainsKey(id));
			info.Id = id;
			info.IsEquipped = _loadout.TryGetValue(info.Equipment.GameId.GetSlot(), out var equipId) && equipId == id;
			return info;
		}

		public EquipmentInfo GetInfo(Equipment equipment, bool isNft)
		{
			var nextEquipment = equipment;
			nextEquipment.Level++;
			
			var durability =
				equipment.GetCurrentDurability(isNft, GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>(),
					GameLogic.TimeService.DateTimeUtcNow.Ticks);
			
			return new EquipmentInfo
			{
				Equipment = equipment,
				ScrappingValue = GetScrappingReward(equipment, isNft),
				UpgradeCost = GetUpgradeCost(equipment, isNft),
				RepairCost = GetRepairCost(equipment, isNft),
				CurrentDurability = durability,
				IsNft = isNft,
				Stats = equipment.GetStats(GameLogic.ConfigsProvider),
				NextLevelStats = nextEquipment.GetStats(GameLogic.ConfigsProvider)
			};
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
			var config = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var timestamp = GameLogic.TimeService.DateTimeUtcNow.Ticks;

			foreach (var (slot, id) in _loadout)
			{
				var contains = _nftInventory.ContainsKey(id);
				
				if (filter == EquipmentFilter.NftOnly && !contains || filter == EquipmentFilter.NoNftOnly && contains)
				{
					continue;
				}

				var durability = _inventory[id].GetCurrentDurability(contains, config, timestamp);

				if (filter == EquipmentFilter.Broken && durability > 0 ||
				    filter == EquipmentFilter.Unbroken && durability == 0)
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
			var config = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var timestamp = GameLogic.TimeService.DateTimeUtcNow.Ticks;

			foreach (var (id, equipment) in _inventory)
			{
				var contains = _nftInventory.ContainsKey(id);
				
				if (filter == EquipmentFilter.NftOnly && !contains || filter == EquipmentFilter.NoNftOnly && contains)
				{
					continue;
				}

				var durability = _inventory[id].GetCurrentDurability(contains, config, timestamp);

				if (filter == EquipmentFilter.Broken && durability > 0 ||
				    filter == EquipmentFilter.Unbroken && durability == 0)
				{
					continue;
				}
				
				ret.Add(GetInfo(id));
			}

			return ret;
		}

		public bool EnoughLoadoutEquippedToPlay()
		{
			return Loadout.Count >= GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>().NftRequiredEquippedForPlay;
		}

		public Equipment GenerateEquipmentFromConfig(EquipmentRewardConfig config)
		{
			var gameId = config.GameId;

			if (gameId.IsInGroup(GameIdGroup.Core))
			{
				var equipmentConfigs = GameLogic.ConfigsProvider.GetConfigsList<QuantumBaseEquipmentStatConfig>();
				var equipmentCategory = config.EquipmentCategory.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.EquipmentCategory));
				var matchingEquipment =  equipmentConfigs.Where(x =>x.Id.IsInGroup(equipmentCategory)).ToList();
				gameId = matchingEquipment[GameLogic.RngLogic.Range(0, matchingEquipment.Count)].Id;
			}
			
			var rarity = config.Rarity.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Rarity));
			var grade = config.Grade.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Grade));
			var adjective = config.Adjective.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Adjective));
			var faction = config.Faction.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Faction));
			var material = config.Material.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Material));
			var edition = config.Edition.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Edition));
			var maxDurability = (uint) GameLogic.RngLogic.Range(config.MaxDurability.Key, config.MaxDurability.Value);
			
			return new Equipment(gameId: gameId,
			                     rarity: rarity,
			                     adjective: adjective,
			                     grade: grade, 
			                     faction: faction,
			                     material: material,
			                     edition: edition,
			                     maxDurability: maxDurability,
			                     level: config.Level,
			                     generation: config.Generation,
			                     tuning: config.Tuning,
			                     initialReplicationCounter: config.InitialReplicationCounter,
			                     replicationCounter: config.InitialReplicationCounter,
			                     lastRepairTimestamp: DateTime.UtcNow.Ticks);
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
			var config = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();

			if (!Inventory.TryGetValue(itemId, out var equipment))
			{
				throw new LogicException($"The player does not own item '{itemId}'");
			}
			
			if (equipment.GetCurrentDurability(NftInventory.ContainsKey(itemId), config, GameLogic.TimeService.DateTimeUtcNow.Ticks) == 0)
			{
				throw new LogicException($"Item '{itemId}' is broken");
			}

			if (_loadout.TryGetValue(slot, out var equippedId))
			{
				if (equippedId == itemId)
				{
					throw new LogicException($"The player already has the given item Id '{itemId}' equipped");
				}

				_loadout[slot] = itemId;
			}
			else
			{
				_loadout.Add(slot, itemId);
			}
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

		public Pair<GameId, uint> Scrap(UniqueId itemId)
		{
			var equipment = _inventory[itemId];
			var reward = GetScrappingReward(equipment, false);

			if (_nftInventory.ContainsKey(itemId))
			{
				throw new LogicException($"Not allowed to scrap NFT items on the client, only on the hub and {itemId} is a NFT");
			}

			RemoveFromInventory(itemId);

			return reward;
		}

		public void Upgrade(UniqueId itemId)
		{
			var equipment = _inventory[itemId];

			if (_nftInventory.ContainsKey(itemId))
			{
				throw new LogicException($"Not allowed to scrap NFT items on the client, only on the hub and {itemId} is a NFT");
			}
			
			if (equipment.IsMaxLevel())
			{
				throw new LogicException($"Item {itemId} is already at max level {equipment.Level} and cannot be upgraded further");
			}

			equipment.Level++;

			_inventory[itemId] = equipment;
		}

		public void Repair(UniqueId itemId)
		{
			var equipment = _inventory[itemId];
			var config = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var durability = equipment.GetCurrentDurability(false, config, GameLogic.TimeService.DateTimeUtcNow.Ticks);

			if (_nftInventory.ContainsKey(itemId))
			{
				throw new LogicException($"Not allowed to scrap NFT items on the client, only on the hub and {itemId} is a NFT");
			}
			
			if (durability == equipment.MaxDurability)
			{
				throw new LogicException($"Item {itemId} is already fully repaired");
			}

			equipment.TotalRestoredDurability += equipment.MaxDurability - durability;
			equipment.LastRepairTimestamp = GameLogic.TimeService.DateTimeUtcNow.Ticks;

			_inventory[itemId] = equipment;
		}

		private int GetWeightedRandomDictionaryIndex<TKey, TValue>(SerializedDictionary<TKey, TValue> dictionary)
		{
			var rangeDictionary = dictionary as Dictionary<TKey, FP>;
			var indexRanges = new List<Tuple<FP, FP>>();
			var currentRangeMax = FP._0;
			
			foreach (var valueMax in rangeDictionary.Values)
			{
				var min = currentRangeMax;
				var max = min + valueMax;
				indexRanges.Add(new Tuple<FP, FP>(min,max));

				currentRangeMax = max;
			}

			var rand = GameLogic.RngLogic.Range(0, currentRangeMax);
			
			foreach (var range in indexRanges)
			{
				if (rand >= range.Item1 && rand < range.Item2)
				{
					return indexRanges.IndexOf(range);
				}
			}

			throw new LogicException("Dictionary weighted random could not return a valid index.");
		}

		private bool RemoveFromInventory(UniqueId equipment)
		{
			if (!_inventory.ContainsKey(equipment))
			{
				throw new LogicException($"The given '{equipment}' id is not in the inventory");
			}

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
	}
}
