using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumSpecialConfig
	{
		public GameId Id;
		public SpecialType SpecialType;
		public IndicatorVfxId Indicator;
		public FP Cooldown;
		public FP Radius;
		public FP PowerAmount;
		public FP Speed;
		public FP MinRange;
		public FP MaxRange;

		public bool IsAimable => MaxRange > FP._0;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumSpecialConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumSpecialConfigs
	{
		public List<QuantumSpecialConfig> QuantumConfigs = new List<QuantumSpecialConfig>();
		
		private IDictionary<GameId, QuantumSpecialConfig> _dictionary = new Dictionary<GameId, QuantumSpecialConfig>();

		/// <summary>
		/// Requests the <see cref="QuantumSpecialConfig"/> of the given enemy <paramref name="gameId"/>
		/// </summary>
		public QuantumSpecialConfig GetConfig(GameId gameId)
		{
			if (_dictionary.Count == 0)
			{
				foreach (var config in QuantumConfigs)
				{
					_dictionary.Add(config.Id, config);
				}
			}

			return _dictionary[gameId];
		}
	}
}