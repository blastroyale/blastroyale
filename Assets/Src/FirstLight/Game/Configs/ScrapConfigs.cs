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
		public GameId ResourceType;
		public uint BaseValue;
		public FP GrowthMultiplier;
		public FP AdjectiveCostK;
		public FP GradeMultiplier;
		public FP LevelMultiplier;
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