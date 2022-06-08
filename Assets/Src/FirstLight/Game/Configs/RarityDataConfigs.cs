using System;
using System.Collections.Generic;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct RarityDataConfig
	{
		public EquipmentRarity Rarity;
		public FP PoolCapacityModifier;
		public int MaxLevel;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="RarityDataConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "RarityDataConfigs", menuName = "ScriptableObjects/Configs/RarityDataConfigs")]
	public class RarityDataConfigs : ScriptableObject, IConfigsContainer<RarityDataConfig>
	{
		[SerializeField] private List<RarityDataConfig> _configs = new List<RarityDataConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<RarityDataConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}