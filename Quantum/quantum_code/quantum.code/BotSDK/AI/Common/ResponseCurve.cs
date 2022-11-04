using Photon.Deterministic;
using System;

namespace Quantum
{
	[BotSDKHidden]
	[System.Serializable]
	public unsafe partial class ResponseCurve : AIFunction<FP>
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public AIParamFP Input;

		[BotSDKHidden]
		public FPAnimationCurve Curve;
		
		public bool Clamp01 = true;

		public FP MultiplyFactor = 1;

		// ========== AssetObject INTERFACE ===========================================================================

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			base.Loaded(resourceManager, allocator);
		}

		// ========== AIFunctionFP INTERFACE ==========================================================================

		public override FP Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			return Execute((FrameThreadSafe)frame, entity, ref aiContext);
		}

		public override FP Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
		{
			if (Input.FunctionRef == default) return 0;

			FP input = Input.ResolveFunction(frame, entity, ref aiContext);
			FP result = Curve.Evaluate(input);

			if(Clamp01 == true)
			{
				if (result > 1) result = 1;
				else if (result < 0) result = 0;
			}

			return result * MultiplyFactor;
		}
	}
}
