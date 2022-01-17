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
		// ========== PROTECTED METHODS ===============================================================================

		protected override BTStatus OnUpdate(BTParams btParams)
		{
			BTStatus status = BTStatus.Success;

			while (GetCurrentChild(btParams.FrameThreadSafe, btParams.Agent) < _childInstances.Length)
			{
				var currentChildId = GetCurrentChild(btParams.FrameThreadSafe, btParams.Agent);
				var child = _childInstances[currentChildId];
				status = child.RunUpdate(btParams);

				if (status == BTStatus.Abort)
				{
					if (btParams.Agent->IsAborting == true)
					{
						return BTStatus.Abort;
					}
					else
					{
						return BTStatus.Failure;
					}
				}

				if (status == BTStatus.Success)
				{
					SetCurrentChild(btParams.FrameThreadSafe, currentChildId + 1, btParams.Agent);
				}
				else
				{
					break;
				}
			}

			return status;
		}

		// ========== INTERNAL METHODS ================================================================================

		internal override void ChildCompletedRunning(BTParams btParams, BTStatus childResult)
		{
			if (childResult == BTStatus.Abort)
			{
				return;
			}

			if (childResult == BTStatus.Failure)
			{
				SetCurrentChild(btParams.FrameThreadSafe, _childInstances.Length, btParams.Agent);

				// If the child failed, then we already know that this sequence failed, so we can force it
				SetStatus(btParams.FrameThreadSafe, BTStatus.Failure, btParams.Agent);

				// Trigger the debugging callbacks
				BTManager.OnNodeFailure?.Invoke(btParams.Entity, Guid.Value);
				BTManager.OnNodeExit?.Invoke(btParams.Entity, Guid.Value);
			}
			else
			{
				var currentChild = GetCurrentChild(btParams.FrameThreadSafe, btParams.Agent);
				SetCurrentChild(btParams.FrameThreadSafe, currentChild + 1, btParams.Agent);
			}
		}
	}
}