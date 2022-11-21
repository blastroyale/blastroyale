using System;

namespace Quantum
{
	[Serializable]
	public unsafe partial class BTForceResult : BTDecorator
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public BTStatus Result;

		// ========== BTNode INTERFACE ================================================================================

		protected override BTStatus OnUpdate(BTParams btParams, ref AIContext aiContext)
		{
			if (_childInstance != null)
				_childInstance.RunUpdate(btParams, ref aiContext);

			return Result;
		}

		public override Boolean DryRun(BTParams btParams, ref AIContext aiContext)
		{
			return true;
		}
	}
}
