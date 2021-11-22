using System;

namespace Quantum
{

  /// <summary>
  /// The selector task is similar to an or operation. It will return success as soon as one of its child tasks return success.
  /// If a child task returns failure then it will sequentially run the next task. If no child task returns success then it will return failure.
  /// </summary>
  [Serializable]
  public unsafe partial class BTSelector : BTComposite
  {
    protected override BTStatus OnUpdate(BTParams p)
    {
      BTStatus status = BTStatus.Success;

      while (GetCurrentChild(p.Frame, p.BtAgent) < _childInstances.Length)
      {
        var currentChildId = GetCurrentChild(p.Frame, p.BtAgent);
        var child = _childInstances[currentChildId];

        status = child.RunUpdate(p);
        if (status == BTStatus.Failure)
        {
          SetCurrentChild(p.Frame, currentChildId + 1, p.BtAgent);
        }
        else
          break;
      }

      return status;
    }

    internal override void ChildCompletedRunning(BTParams p, BTStatus childResult)
    {
      if (childResult == BTStatus.Failure)
      {
        var currentChild = GetCurrentChild(p.Frame, p.BtAgent);
        SetCurrentChild(p.Frame, currentChild + 1, p.BtAgent);
      }
      else
      {
        SetCurrentChild(p.Frame, _childInstances.Length + 1, p.BtAgent);

        // If the child succeeded, then we already know that this sequence succeeded, so we can force it
        SetStatus(p.Frame, BTStatus.Success, p.BtAgent);

        // Trigger the debugging callbacks
        BTManager.OnNodeSuccess?.Invoke(p.Entity, Guid.Value);
        BTManager.OnNodeExit?.Invoke(p.Entity, Guid.Value);
      }
    }
  }
}