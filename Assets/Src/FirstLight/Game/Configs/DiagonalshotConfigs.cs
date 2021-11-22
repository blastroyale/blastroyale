using System.Collections.Generic;
using FirstLight.GoogleSheetImporter;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumDiagonalshotConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "DiagonalshotConfigs", menuName = "ScriptableObjects/Configs/DiagonalshotConfigs")]
	public class DiagonalshotConfigs : QuantumDiagonalshotConfigsAsset, IConfigsContainer<QuantumDiagonalshotConfig>
	{
		// ReSharper disable once ConvertToAutoProperty
		public List<QuantumDiagonalshotConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}