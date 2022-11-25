using System;
using System.Collections.Generic;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct RepairDataConfig
	{
		public GameId ResourceType;
		public uint BaseRepairCost;
		public FP BasePower;
		public FP DurabilityCostIncreasePerPoint;
	}
	
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="RepairDataConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "RepairDataConfigs", menuName = "ScriptableObjects/Configs/RepairDataConfigs")]
	public class RepairDataConfigs : ScriptableObject, IConfigsContainer<RepairDataConfig>
	{
		[SerializeField] private List<RepairDataConfig> _configs = new List<RepairDataConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<RepairDataConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}