using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct EditionDataConfig
	{
		public EquipmentEdition Edition;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="EditionDataConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "EditionDataConfigs", menuName = "ScriptableObjects/Configs/EditionDataConfigs")]
	public class EditionDataConfigs : ScriptableObject, IConfigsContainer<EditionDataConfig>
	{
		[SerializeField] private List<EditionDataConfig> _configs = new List<EditionDataConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<EditionDataConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}