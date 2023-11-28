using System;
using System.Collections.Generic;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// This struct stores a "Chest of Equipment"
	/// That means it defines base rules to generate a given equipment
	/// The rewards are non-NFTs, and are generated from a list of possibilities and ranges (GameID + Chance)
	/// </summary>
	[Serializable]
	public struct EquipmentRewardConfig
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
		public int Amount;
	}
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="EquipmentRewardConfigs"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "BattlePassRewardConfigs",
	                 menuName = "ScriptableObjects/Configs/EquipmentRewardConfigs")]
	public class EquipmentRewardConfigs : ScriptableObject, IConfigsContainer<EquipmentRewardConfig>
	{
		[SerializeField] private List<EquipmentRewardConfig> _configs;

		public List<EquipmentRewardConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}