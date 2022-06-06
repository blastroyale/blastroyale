using System;
using System.Collections.Generic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumMapConfig
	{
		public int Id;
		public GameId Map;
		public GameMode GameMode;
		public int PlayersLimit;
		public uint GameEndTarget;
		public bool IsTestMap;
	}
	
	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumMapConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumMapConfigs
	{
		public List<QuantumMapConfig> QuantumConfigs = new List<QuantumMapConfig>();
		
		private IDictionary<int, QuantumMapConfig> _dictionary = new Dictionary<int, QuantumMapConfig>();

		/// <summary>
		/// Requests the <see cref="QuantumMapConfig"/> from it's <paramref name="id"/>
		/// </summary>
		public QuantumMapConfig GetConfig(int id)
		{
			if (_dictionary.Count == 0)
			{
				foreach (var config in QuantumConfigs)
				{
					_dictionary.Add(config.Id, config);
				}
			}

			return _dictionary[id];
		}
	}
}