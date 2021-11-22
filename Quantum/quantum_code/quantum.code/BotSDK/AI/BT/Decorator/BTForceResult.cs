using System;

namespace Quantum
{
  [Serializable]
  public unsafe partial class BTForceResult : BTDecorator
  {
    public BTStatus Result;

    protected override BTStatus OnUpdate(BTParams p)
    {
      if (_childInstance != null)
        _childInstance.RunUpdate(p);

      return Result;
    }

    public override Boolean DryRun(BTParams p)
    {
      return true;
    }
  }
}
