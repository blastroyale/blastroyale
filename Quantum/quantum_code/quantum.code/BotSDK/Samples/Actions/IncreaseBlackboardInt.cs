using System;

namespace Quantum
{
  [Serializable]
  [AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
  public unsafe partial class IncreaseBlackboardInt : AIAction
  {
    public AIBlackboardValueKey Key;
    public AIParamInt IncrementAmount; 

    public override unsafe void Update(Frame f, EntityRef e)
    {
      var bbComponent = f.Unsafe.GetPointer<AIBlackboardComponent>(e);

      var hfsmAgent = f.Unsafe.GetPointer<HFSMAgent>(e);
      var config = hfsmAgent->GetConfig(f);

      var incrementValue = IncrementAmount.Resolve(f, bbComponent, config);

      var currentAmount = bbComponent->GetInteger(f, Key.Key);
      currentAmount += incrementValue;

      bbComponent->Set(f, Key.Key, currentAmount);
    }
  }
}