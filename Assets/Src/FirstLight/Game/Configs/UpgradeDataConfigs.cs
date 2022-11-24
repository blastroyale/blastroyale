using System;
using System.Collections.Generic;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct UpgradeDataConfig
	{
		public GameId ResourceType;
		public uint BaseValue;
		public FP GrowthMultiplier;
		public FP AdjectiveCostK;
		public uint AdjectiveCostScale;
		public FP GradeMultiplier;
		public FP LevelMultiplier;
		public uint DurabilityDivider;
	}
	
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="UpgradeDataConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "UpgradeDataConfigs", menuName = "ScriptableObjects/Configs/UpgradeDataConfigs")]
	public class UpgradeDataConfigs : ScriptableObject, IConfigsContainer<UpgradeDataConfig>
	{
		[SerializeField] private List<UpgradeDataConfig> _configs = new List<UpgradeDataConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<UpgradeDataConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}