using System;
using Photon.Deterministic;

namespace Quantum
{
  [Serializable]
  [AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
  public partial class TimerDecision : HFSMDecision
  {
    public AIParamFP TimeToTrueState = FP._3;

    public override unsafe bool Decide(Frame f, EntityRef e)
    {
      var bbComponent = f.Has<AIBlackboardComponent>(e) ? f.Get<AIBlackboardComponent>(e) : default;
      
      var hfsmAgent = f.Unsafe.GetPointer<HFSMAgent>(e);
      var config = hfsmAgent->GetConfig(f);

      FP requiredTime = TimeToTrueState.Resolve(f, &bbComponent, config);

      var hfsmData = &hfsmAgent->Data;
      return hfsmData->Time >= requiredTime;
    }
  }
}
