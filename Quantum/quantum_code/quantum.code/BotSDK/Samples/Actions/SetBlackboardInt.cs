using System;

namespace Quantum
{
  [Serializable]
  [AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
  public unsafe partial class SetBlackboardInt : AIAction
  {
    public AIBlackboardValueKey Key;
    public AIParamInt Value;

    public override unsafe void Update(Frame f, EntityRef e)
    {
      var bbComponent = f.Unsafe.GetPointer<AIBlackboardComponent>(e);

      var hfsmAgent = f.Unsafe.GetPointer<HFSMAgent>(e);
      var config = hfsmAgent->GetConfig(f);

      var value = Value.Resolve(f, bbComponent, config);
      bbComponent->Set(f, Key.Key, value);
    }
  }
}