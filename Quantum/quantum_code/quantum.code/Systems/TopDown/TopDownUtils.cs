using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	public static unsafe class TopDownUtils
	{
		
		public static ref FPVector2 GetKccAimDirection(this EntityRef entity, Frame f)
		{
			if (!f.Unsafe.TryGetPointer<TopDownController>(entity, out var kcc))
			{
				throw new Exception($"Entity {entity} have no kcc");
			}
			return ref kcc->AimDirection;
		}
		
		public static ref FPVector2 GetKccMoveDirection(this EntityRef entity, Frame f)
		{
			if (!f.Unsafe.TryGetPointer<TopDownController>(entity, out var kcc))
			{
				throw new Exception($"Entity {entity} have no kcc");
			}
			return ref kcc->MoveDirection;
		}
	}
}