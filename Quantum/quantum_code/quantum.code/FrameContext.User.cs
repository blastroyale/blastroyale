using System;
using System.Collections.Generic;

namespace Quantum 
{
	public unsafe partial class FrameContextUser
	{
		public QuantumMapConfig MapConfig { get; internal set; }
		public int TargetAllLayerMask { get; internal set; }
		
		public EquipmentRarity MedianRarity { get; internal set; }
		public Equipment[] PlayerWeapons { get; internal set; }
	}
}