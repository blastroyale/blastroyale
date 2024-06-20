using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct MaterialDataConfig
	{
		public EquipmentMaterial Material;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="MaterialDataConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "MaterialDataConfigs", menuName = "ScriptableObjects/Configs/MaterialDataConfigs")]
	public class MaterialDataConfigs : ScriptableObject, IConfigsContainer<MaterialDataConfig>
	{
		[SerializeField] private List<MaterialDataConfig> _configs = new List<MaterialDataConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<MaterialDataConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}