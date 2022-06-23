using System;
using System.Collections.Generic;
using System.Linq;

namespace Quantum
{
	public unsafe partial class FrameContextUser
	{
		public QuantumMapConfig MapConfig { get; internal set; }
		public int TargetAllLayerMask { get; internal set; }
		
		private EquipmentRarity _medianRarity;
		private Equipment[] _offhandPool;
		
		public EquipmentRarity GetMedianRarity(Frame f)
		{
			if (_offhandPool == null)
			{
				QuantumHelpers.CalculateOffhandData(f, out _medianRarity, out _offhandPool);
			}

			return _medianRarity;
		}

		public Equipment[] GetOffhandPool(Frame f)
		{
			if (_offhandPool == null)
			{
				QuantumHelpers.CalculateOffhandData(f, out _medianRarity, out _offhandPool);
			}

			return _offhandPool;
		}

	}
}