using System;
using Photon.Deterministic;

namespace Quantum
{
  public abstract partial class HFSMLogicalDecision : HFSMDecision
  {
    public AssetRefHFSMDecision[] Decisions;

    protected HFSMDecision[] _decisions;

    public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
    {
      base.Loaded(resourceManager, allocator);

      _decisions = new HFSMDecision[Decisions == null ? 0 : Decisions.Length];
      if (Decisions != null)
      {
        for (Int32 i = 0; i < Decisions.Length; i++)
        {
          _decisions[i] = (HFSMDecision)resourceManager.GetAsset(Decisions[i].Id);
        }
      }
    }
  }

  [Serializable]
  [AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
  public partial class HFSMOrDecision : HFSMLogicalDecision
  {
    public override unsafe bool Decide(Frame f, EntityRef e)
    {
      foreach (var decision in _decisions)
      {
        if (decision.Decide(f, e))
          return true;
      }
      return false;
    }
  }


  [Serializable]
  [AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
  public partial class HFSMAndDecision : HFSMLogicalDecision
  {
    public override unsafe bool Decide(Frame f, EntityRef e)
    {
      foreach (var decision in _decisions)
      {
        if (!decision.Decide(f, e))
          return false;
      }
      return true;
    }
  }


  [Serializable]
  [AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
  public partial class HFSMNotDecision : HFSMLogicalDecision
  {
    public override unsafe bool Decide(Frame f, EntityRef e)
    {
      return !_decisions[0].Decide(f, e);
    }
  }
}
