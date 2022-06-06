using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Infos;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="MapConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "MapConfigs", menuName = "ScriptableObjects/Configs/MapConfigs")]
	public class MapConfigs : QuantumMapConfigsAsset, IConfigsContainer<QuantumMapConfig>
	{
		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<QuantumMapConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}