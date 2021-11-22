using Photon.Deterministic;
using System;

namespace Quantum
{
  [BotSDKHidden]
  [System.Serializable]
  public unsafe partial class ResponseCurve : AIFunctionFP
  {
    public AIParamFP Input;

    [BotSDKHidden]
    public FPAnimationCurve Curve;

    public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
    {
      base.Loaded(resourceManager, allocator);
    }

    public override FP Execute(Frame frame, EntityRef entity = default)
    {
      if (Input.FunctionRef == default) return 0;

      FP input = Input.ResolveFunc(frame, entity);
      FP result = Curve.Evaluate(input);

      if (result > 1) result = 1;
      else if (result < 0) result = 0;

      return result;
    }
  }
}
