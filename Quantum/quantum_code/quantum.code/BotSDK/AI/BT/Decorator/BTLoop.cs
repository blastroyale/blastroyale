using Photon.Deterministic;
using System;

namespace Quantum
{
	[Serializable]
	public unsafe partial class BTLoop : BTDecorator
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public Int32 LoopIterations;
		public Boolean LoopForever;
		public FP LoopTimeout = -FP._1;

		public BTDataIndex StartTimeIndex;
		public BTDataIndex IterationCountIndex;

		// ========== BTNode INTERFACE ================================================================================

		public override void Init(Frame frame, AIBlackboardComponent* blackboard, BTAgent* agent)
		{
			base.Init(frame, blackboard, agent);

			agent->AddFPData(frame, 0);
			agent->AddIntData(frame, 0);
		}

		public override void OnEnter(BTParams btParams)
		{
			base.OnEnter(btParams);

			var frame = btParams.Frame;
			var currentTime = frame.DeltaTime * frame.Number;

			btParams.Agent->SetFPData(frame, currentTime, StartTimeIndex.Index);
			btParams.Agent->SetIntData(frame, 0, IterationCountIndex.Index);
		}

		protected override BTStatus OnUpdate(BTParams btParams)
		{
			var frame = btParams.Frame;

			int iteration = btParams.Agent->GetIntData(frame, IterationCountIndex.Index) + 1;
			btParams.Agent->SetIntData(frame, iteration, IterationCountIndex.Index);

			if (DryRun(btParams) == false)
			{
				return BTStatus.Success;
			}

			var childResult = BTStatus.Failure;
			if (_childInstance != null)
			{
				_childInstance.SetStatus(btParams.Frame, BTStatus.Inactive, btParams.Agent);
				childResult = _childInstance.RunUpdate(btParams);
			}

			return childResult;
		}

		public override Boolean DryRun(BTParams btParams)
		{
			if (LoopForever && LoopTimeout < FP._0)
			{
				return true;
			}
			else if (LoopForever)
			{
				var frame = btParams.Frame;
				FP startTime = btParams.Agent->GetFPData(frame, StartTimeIndex.Index);

				var currentTime = frame.DeltaTime * frame.Number;
				if (currentTime < startTime + LoopTimeout)
				{
					return true;
				}
			}
			else
			{
				var frame = btParams.Frame;
				int iteration = btParams.Agent->GetIntData(frame, IterationCountIndex.Index);
				if (iteration <= LoopIterations)
				{
					return true;
				}
			}

			return false;
		}
	}
}
