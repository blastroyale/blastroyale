using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct RarityConfig
	{
		public ItemRarity Rarity;
		public uint MaxLevel;
		public uint FusionCost;
		public uint EnhancementCost;
		public uint EnhancementItemAmount;
		public uint SellBasePrice;
		public float SellPriceLevelPowerOf;
		public uint UpgradeBasePrice;
		public float UpgradePriceLevelPowerOf;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="RarityConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "RarityConfigs", menuName = "ScriptableObjects/Configs/RarityConfigs")]
	public class RarityConfigs : ScriptableObject, IConfigsContainer<RarityConfig>
	{
		[SerializeField] private List<RarityConfig> _configs = new List<RarityConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<RarityConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}