using System.Collections.Generic;

namespace Quantum
{
	public unsafe partial class FrameContextUser
	{
		public Mutator Mutators;

		public QuantumMapConfig MapConfig { get; internal set; }
		
		public QuantumGameModeConfig GameModeConfig { get; internal set; }

		public IDictionary<int, QuantumShrinkingCircleConfig> MapShrinkingCircleConfigs { get; internal set; }
		
		public int TargetAllLayerMask { get; internal set; }
		public int TargetMapOnlyLayerMask { get; internal set; }
		public int TargetMapAndPlayersMask { get; internal set; }
		public int TargetPlayerLineOfSightLayerMask { get; internal set; }
		public int TargetPlayersMask { get; internal set; }
		public int TargetPlayerTriggersLayerIndex { get; internal set; }
	}
}