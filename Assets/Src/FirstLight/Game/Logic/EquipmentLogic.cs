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
using Quantum;
using UnityEngine;
using Random = System.Random;

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
		IObservableDictionaryReader<UniqueId, Quantum.Equipment> Inventory { get; }

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
		Dictionary<EquipmentStatType, float> GetEquipmentStats(Quantum.Equipment equipment);

		/// <summary>
		/// Requests to see if player has enough NFTs equipped for play
		/// </summary>
		bool EnoughLoadoutEquippedToPlay();

		/// <summary>
		/// Generates a new unique non-NFT piece of equipment from battle pass reward configs
		/// </summary>
		Quantum.Equipment GenerateEquipmentFromBattlePassReward(BattlePassRewardConfig config);
	}

	/// <inheritdoc />
	public interface IEquipmentLogic : IEquipmentDataProvider
	{
		/// <summary>
		/// Adds an item to the inventory and assigns it a new UniqueId.
		/// </summary>
		UniqueId AddToInventory(Quantum.Equipment equipment);

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
		private IObservableDictionary<UniqueId, Quantum.Equipment> _inventory;
		private IObservableDictionary<UniqueId, NftEquipmentData> _nftInventory;
		public IObservableDictionaryReader<GameIdGroup, UniqueId> Loadout => _loadout;
		public IObservableDictionaryReader<UniqueId, Quantum.Equipment> Inventory => _inventory;
		public IObservableDictionaryReader<UniqueId, NftEquipmentData> NftInventory => _nftInventory;

		public EquipmentLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		public void Init()
		{
			_loadout = new ObservableDictionary<GameIdGroup, UniqueId>(DataProvider.GetData<PlayerData>().Equipped);
			_inventory = new ObservableDictionary<UniqueId, Quantum.Equipment>(Data.Inventory);
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

		public Dictionary<EquipmentStatType, float> GetEquipmentStats(Quantum.Equipment equipment)
		{
			return equipment.GetStats(GameLogic.ConfigsProvider);
		}

		public bool EnoughLoadoutEquippedToPlay()
		{
			return Loadout.Count >= GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>().NftRequiredEquippedForPlay;
		}

		public Quantum.Equipment GenerateEquipmentFromBattlePassReward(BattlePassRewardConfig config)
		{
			Random r = new Random();

			var gameId = config.GameId;

			if (gameId.IsInGroup(GameIdGroup.Core))
			{
				var equipmentConfigs = GameLogic.ConfigsProvider.GetConfigsList<QuantumBaseEquipmentStatConfig>();
				var equipmentCategory = config.EquipmentCategory.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.EquipmentCategory));
				var matchingEquipment =  equipmentConfigs.Where(x =>x.Id.IsInGroup(equipmentCategory)).ToList();
				gameId = matchingEquipment[r.Next(0, matchingEquipment.Count())].Id;
			}
			
			var rarity = config.Rarity.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Rarity));
			var grade = config.Grade.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Grade));
			var adjective = config.Adjective.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Adjective));
			var faction = config.Faction.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Faction));
			var material = config.Material.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Material));
			var edition = config.Edition.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Edition));
			var maxDurability = (uint) r.Next(config.MaxDurability.Key, config.MaxDurability.Value);
			
			return new Quantum.Equipment(gameId,
			                             rarity: rarity,
			                             adjective: adjective,
			                             grade: grade, 
			                             faction: faction,
			                             material: material,
			                             edition: edition,
			                             maxDurability: maxDurability,
			                             durability: maxDurability,
			                             level: config.Level,
			                             generation: config.Generation,
			                             tuning: config.Tuning,
			                             initialReplicationCounter: config.InitialReplicationCounter,
			                             replicationCounter: config.InitialReplicationCounter
			                            );
		}

		private int GetWeightedRandomDictionaryIndex<TKey, TValue>(SerializedDictionary<TKey, TValue> dictionary)
		{
			Dictionary<TKey, float> rangeDictionary = dictionary as Dictionary<TKey, float>;
			List<Tuple<float, float>> indexRanges = new List<Tuple<float, float>>();

			var currentRangeMax = 0f;
			
			foreach (var valueMax in rangeDictionary.Values)
			{
				var min = currentRangeMax;
				var max = min + valueMax;
				indexRanges.Add(new Tuple<float, float>(min,max));

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

		public UniqueId AddToInventory(Quantum.Equipment equipment)
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
			var slots = Quantum.Equipment.EquipmentSlots;

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