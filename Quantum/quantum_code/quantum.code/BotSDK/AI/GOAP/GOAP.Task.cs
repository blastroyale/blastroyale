using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
  [System.SerializableAttribute()]
  public partial struct GOAPState
  {
  }

  public unsafe partial class GOAPTask
  {
    public string Label;
    public GOAPState Consequences;
    public GOAPState Conditions;

    public AssetRefAIAction[] Actions;

    [NonSerialized]
    public AIAction[] _actions;

    public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
    {
      base.Loaded(resourceManager, allocator);

      _actions = new AIAction[Actions == null ? 0 : Actions.Length];
      for (Int32 i = 0; i < _actions.Length; i++)
      {
        _actions[i] = (AIAction)resourceManager.GetAsset(Actions[i].Id);
      }
    }

    public void Update(Frame f, EntityRef e)
    {
      for (Int32 i = 0; i < _actions.Length; i++)
      {
        _actions[i].Update(f, e);
      }
    }
  }
}
