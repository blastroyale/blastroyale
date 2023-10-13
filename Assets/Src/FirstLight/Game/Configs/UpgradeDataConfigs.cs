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
		public uint Level;
		public uint CoinCost;
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