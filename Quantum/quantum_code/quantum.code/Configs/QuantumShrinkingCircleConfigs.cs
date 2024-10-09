using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public class QuantumShrinkingCircleConfig
	{
		public int Key;
		public int Step;
		public GameId Map;

		public FP DelayTime;
		public FP WarningTime;
		public FP ShrinkingTime;
		public FP ShrinkingSizeK;
		
		// 1 means new safe area will fit and may touch edges
		// 0 (min value) means new safe area will be exactly in the center
		// 2 means new safe area will be able to go half of its radius outside of the current safe area
		public FP NewSafeSpaceAreaSizeK;
		
		public FP MaxHealthDamage;
		public FP AirdropChance;
		public QuantumPair<FP, FP> AirdropStartTimeRange;
		public FP AirdropDropDuration;
		public GameId AirdropChest;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumShrinkingCircleConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumShrinkingCircleConfigs
	{
		public List<QuantumShrinkingCircleConfig> QuantumConfigs = new List<QuantumShrinkingCircleConfig>();
		
		private IDictionary<string, IDictionary<int, QuantumShrinkingCircleConfig>> _dictionary;

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			var dictionary = new Dictionary<string, IDictionary<int, QuantumShrinkingCircleConfig>>();
			foreach (var config in QuantumConfigs)
			{
				if (!dictionary.ContainsKey(config.Map.ToString()))
				{
					dictionary.Add(config.Map.ToString(), new Dictionary<int, QuantumShrinkingCircleConfig>());
				}

				// We do -1 here so dictionary keys can acts as indexes, starting from 0 (as steps in configs start with 1)
				dictionary[config.Map.ToString()].Add(config.Step - 1, config);
			}
			_dictionary = dictionary;
		}
		
		/// <summary>
		/// Requests the <see cref="QuantumMapConfig"/> from it's <paramref name="id"/>
		/// </summary>
		public IDictionary<int, QuantumShrinkingCircleConfig> GetConfigs(string mapId)
		{
			if (_dictionary.TryGetValue(mapId,out var configs))
			{
				return configs;
			}

			return new Dictionary<int, QuantumShrinkingCircleConfig>();
		}
	}
}