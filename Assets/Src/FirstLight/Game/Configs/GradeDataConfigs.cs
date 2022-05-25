using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct GradeDataConfig
	{
		public EquipmentGrade Grade;
		public double PoolIncreaseModifier;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="GradeDataConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "GradeDataConfigs", menuName = "ScriptableObjects/Configs/GradeDataConfigs")]
	public class GradeDataConfigs : ScriptableObject, IConfigsContainer<GradeDataConfig>
	{
		[SerializeField] private List<GradeDataConfig> _configs = new List<GradeDataConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<GradeDataConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}