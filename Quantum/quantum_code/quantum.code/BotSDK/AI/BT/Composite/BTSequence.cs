using System;

namespace Quantum
{

  /// <summary>
  /// The sequence task is similar to an and operation. It will return failure as soon as one of its child tasks return failure.
  /// If a child task returns success then it will sequentially run the next task. If all child tasks return success then it will return success.
  /// </summary>
  [Serializable]
  public unsafe partial class BTSequence : BTComposite
  {
    protected override BTStatus OnUpdate(BTParams p)
    {
      BTStatus status = BTStatus.Success;

      while (GetCurrentChild(p.Frame, p.BtAgent) < _childInstances.Length)
      {
        var currentChildId = GetCurrentChild(p.Frame, p.BtAgent);
        var child = _childInstances[currentChildId];

        status = child.RunUpdate(p);
        if (status == BTStatus.Success)
        {
          SetCurrentChild(p.Frame, currentChildId + 1, p.BtAgent);
        }
        else
        {
          break;
        }
      }
      return status;
    }

    internal override void ChildCompletedRunning(BTParams p, BTStatus childResult)
    {
      if (childResult == BTStatus.Failure)
      {
        SetCurrentChild(p.Frame, _childInstances.Length + 1, p.BtAgent);

        // If the child failed, then we already know that this sequence failed, so we can force it
        SetStatus(p.Frame, BTStatus.Failure, p.BtAgent);

        // Trigger the debugging callbacks
        BTManager.OnNodeFailure?.Invoke(p.Entity, Guid.Value);
        BTManager.OnNodeExit?.Invoke(p.Entity, Guid.Value);
      }
      else
      {
        var currentChild = GetCurrentChild(p.Frame, p.BtAgent);
        SetCurrentChild(p.Frame, currentChild + 1, p.BtAgent);
      }
    }
  }
}