using System;
using System.Collections.Generic;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct ScrapConfig
	{
		public EquipmentRarity Rarity;
		public uint FragmentReward;
		public uint CoinReward;
	}
	
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="ScrapConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "ScrapConfigs", menuName = "ScriptableObjects/Configs/ScrapConfigs")]
	public class ScrapConfigs : ScriptableObject, IConfigsContainer<ScrapConfig>
	{
		[SerializeField] private List<ScrapConfig> _configs = new List<ScrapConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<ScrapConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}