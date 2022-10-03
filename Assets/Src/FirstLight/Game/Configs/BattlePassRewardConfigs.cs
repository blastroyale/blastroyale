using System;
using System.Collections.Generic;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// This struct stores the battle pass reward configs.
	/// The rewards are non-NFTs, and are generated from a list of possibilities and ranges (GameID + Chance)
	/// </summary>
	[Serializable]
	public struct BattlePassRewardConfig
	{
		public int Id;
		public GameId GameId;
		public SerializedDictionary<GameIdGroup, FP> EquipmentCategory;
		public SerializedDictionary<EquipmentEdition, FP> Edition;
		public SerializedDictionary<EquipmentRarity, FP> Rarity;
		public SerializedDictionary<EquipmentGrade, FP> Grade;
		public SerializedDictionary<EquipmentFaction, FP> Faction;
		public SerializedDictionary<EquipmentAdjective, FP> Adjective;
		public SerializedDictionary<EquipmentMaterial, FP> Material;
		public Pair<int, int> MaxDurability;
		public uint InitialReplicationCounter;
		public uint Tuning;
		public uint Level;
		public uint Generation;

	}
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="BattlePassRewardConfigs"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "BattlePassRewardConfigs",
	                 menuName = "ScriptableObjects/Configs/BattlePassRewardConfigs")]
	public class BattlePassRewardConfigs : ScriptableObject, IConfigsContainer<BattlePassRewardConfig>
	{
		[SerializeField] private List<BattlePassRewardConfig> _configs;

		public List<BattlePassRewardConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}