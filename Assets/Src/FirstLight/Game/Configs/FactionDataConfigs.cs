using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct FactionDataConfig
	{
		public EquipmentFaction Faction;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="FactionDataConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "FactionDataConfigs", menuName = "ScriptableObjects/Configs/FactionDataConfigs")]
	public class FactionDataConfigs : ScriptableObject, IConfigsContainer<FactionDataConfig>
	{
		[SerializeField] private List<FactionDataConfig> _configs = new List<FactionDataConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<FactionDataConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}