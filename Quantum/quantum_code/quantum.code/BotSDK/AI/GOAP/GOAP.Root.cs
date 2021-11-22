using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
  public partial class GOAPRoot
  {
    public AssetRefGOAPTask[] TasksRefs;
    
    [NonSerialized]
    public GOAPTask[] Tasks;

    public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
    {
      base.Loaded(resourceManager, allocator);

      Tasks = new GOAPTask[TasksRefs == null ? 0 : TasksRefs.Length];
      for (int i = 0; i < TasksRefs.Length; i++)
      {
        Tasks[i] = (GOAPTask)resourceManager.GetAsset(TasksRefs[i].Id);
      }
    }
  }
}