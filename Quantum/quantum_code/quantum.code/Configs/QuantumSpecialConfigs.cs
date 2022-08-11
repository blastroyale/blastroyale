using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	public enum IndicatorVfxId
	{
		None,
		Movement,
		ScalableLine,
		Line,
		Cone,
		Range,
		Radial,
		TOTAL,            // Used to know the total amount of this type without the need of reflection
	}
	
	[Serializable]
	public partial struct QuantumSpecialConfig
	{
		public GameId Id;
		public SpecialType SpecialType;
		public IndicatorVfxId Indicator;
		public FP Cooldown;
		public FP InitialCooldown;
		public FP Radius;
		public FP SpecialPower;
		public FP Speed;
		public FP MinRange;
		public FP MaxRange;
		public uint Knockback;

		public bool IsAimable => MaxRange > FP._0;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumSpecialConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumSpecialConfigs
	{
		public List<QuantumSpecialConfig> QuantumConfigs = new List<QuantumSpecialConfig>();
		
		private IDictionary<GameId, QuantumSpecialConfig> _dictionary = null;

		/// <summary>
		/// Requests the <see cref="QuantumSpecialConfig"/> of the given enemy <paramref name="gameId"/>
		/// </summary>
		public QuantumSpecialConfig GetConfig(GameId gameId)
		{
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<GameId, QuantumSpecialConfig>();
				
				foreach (var config in QuantumConfigs)
				{
					_dictionary.Add(config.Id, config);
				}
			}

			return _dictionary[gameId];
		}
	}
}