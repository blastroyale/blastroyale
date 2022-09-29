using System;
using System.Collections.Generic;
using FirstLight.Game.Utils;
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
		public List<GameIdGroup> EquipmentCategory;
		public SerializedDictionary<EquipmentEdition, float> Edition;
		public SerializedDictionary<EquipmentRarity, float> Rarity;
		public SerializedDictionary<EquipmentGrade, float> Grade;
		public SerializedDictionary<EquipmentFaction, float> Faction;
		public SerializedDictionary<EquipmentAdjective, float> Adjective;
		public SerializedDictionary<EquipmentMaterial, float> Material;
		public Pair<float, float> MaxDurability;
		public int InitialReplicationCounter;
		public int Tuning;
		public int Level;
		public int Generation;

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