using Photon.Deterministic;

namespace Quantum
{
  public abstract unsafe partial class AIFunctionFP
  {
    public abstract FP Execute(Frame frame, EntityRef entity = default);
  }

  [BotSDKHidden]
  [System.Serializable]
  public unsafe partial class DefaultAIFunctionFP : AIFunctionFP
  {
    public override FP Execute(Frame frame, EntityRef entity = default)
    {
      return FP._0;
    }
  }
}
