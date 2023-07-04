using System.Collections.Generic;

namespace Quantum
{
	public unsafe partial class FrameContextUser
	{
		private Dictionary<MutatorType, QuantumMutatorConfig> _mutators;

		public QuantumMapConfig MapConfig { get; internal set; }
		
		public QuantumGameModeConfig GameModeConfig { get; internal set; }

		public List<QuantumMutatorConfig> MutatorConfigs { get; internal set; }
		
		public IDictionary<int, QuantumShrinkingCircleConfig> MapShrinkingCircleConfigs { get; internal set; }
		
		public int TargetAllLayerMask { get; internal set; }
		
		public int TargetPlayersMask { get; internal set; }

		/// <summary>
		/// Requests the current game's mutator by type
		/// </summary>
		public bool TryGetMutatorByType(MutatorType type, out QuantumMutatorConfig quantumMutatorConfig)
		{
			if (_mutators == null)
			{
				_mutators = new Dictionary<MutatorType, QuantumMutatorConfig>();
				foreach (var config in MutatorConfigs)
				{
					_mutators[config.Type] = config;
				}
			}

			return _mutators.TryGetValue(type, out quantumMutatorConfig);
		}
	}
}