using System;
using System.Collections.Generic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumMapConfig
	{
		public int Id;
		public GameId Map;
		public int PlayersLimit;
		public bool IsTestMap;
	}
	
	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumMapConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumMapConfigs
	{
		public List<QuantumMapConfig> QuantumConfigs = new List<QuantumMapConfig>();
		
		private IDictionary<int, QuantumMapConfig> _dictionary = null;

		/// <summary>
		/// Requests the <see cref="QuantumMapConfig"/> from it's <paramref name="id"/>
		/// </summary>
		public QuantumMapConfig GetConfig(int id)
		{
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<int, QuantumMapConfig>();
				
				foreach (var config in QuantumConfigs)
				{
					_dictionary.Add(config.Id, config);
				}
			}

			return _dictionary[id];
		}
	}
}