using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct ManufacturerDataConfig
	{
		public EquipmentManufacturer Manufacturer;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="ManufacturerDataConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "ManufacturerDataConfigs", menuName = "ScriptableObjects/Configs/ManufacturerDataConfigs")]
	public class ManufacturerDataConfigs : ScriptableObject, IConfigsContainer<ManufacturerDataConfig>
	{
		[SerializeField] private List<ManufacturerDataConfig> _configs = new List<ManufacturerDataConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<ManufacturerDataConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}