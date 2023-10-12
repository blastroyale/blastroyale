using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct FuseConfig
	{
		public EquipmentRarity Rarity;
		public uint FragmentCost;
		public uint CoinCost;
	}
	
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="FuseConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "FuseConfigs", menuName = "ScriptableObjects/Configs/FuseConfigs")]
	public class FuseConfigs : ScriptableObject, IConfigsContainer<FuseConfig>
	{
		[SerializeField] private List<FuseConfig> _configs = new List<FuseConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<FuseConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}