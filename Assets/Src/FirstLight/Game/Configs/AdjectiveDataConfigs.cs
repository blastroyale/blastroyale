using System;
using System.Collections.Generic;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct AdjectiveDataConfig
	{
		public EquipmentAdjective Adjective;
		public FP PoolCapacityModifier;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="AdjectiveDataConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "AdjectiveDataConfigs", menuName = "ScriptableObjects/Configs/AdjectiveDataConfigs")]
	public class AdjectiveDataConfigs : ScriptableObject, IConfigsContainer<AdjectiveDataConfig>
	{
		[SerializeField] private List<AdjectiveDataConfig> _configs = new List<AdjectiveDataConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<AdjectiveDataConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}