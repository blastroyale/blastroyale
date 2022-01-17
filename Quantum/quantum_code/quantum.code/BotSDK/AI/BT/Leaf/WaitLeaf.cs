using Photon.Deterministic;
using System;

namespace Quantum
{
	[Serializable]
	public unsafe partial class WaitLeaf : BTLeaf
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		// How many time shall be waited
		// This is measured in seconds
		public FP Duration;

		// Indexer for us to store the End Time value on the BTAgent itself
		public BTDataIndex EndTimeIndex;

		// ========== BTNode INTERFACE ================================================================================

		public override void Init(Frame frame, AIBlackboardComponent* blackboard, BTAgent* agent)
		{
			base.Init(frame, blackboard, agent);

			// We allocate space for the End Time on the Agent so we can change it in runtime
			agent->AddFPData(frame, 0);
		}

		public override void OnEnter(BTParams btParams)
		{
			base.OnEnter(btParams);

			FP currentTime;
			FP endTime;

			// Get the current time
			currentTime = btParams.Frame.BotSDKGameTime();
			// Add the Duration value so we know when the Leaf will stop running
			endTime = currentTime + Duration;

			// Store the final value on the Agent data
			btParams.Agent->SetFPData(btParams.FrameThreadSafe, endTime, EndTimeIndex.Index);
		}

		protected override BTStatus OnUpdate(BTParams btParams)
		{
			FP currentTime;
			FP endTime;

			currentTime = btParams.Frame.BotSDKGameTime();
			endTime = btParams.Agent->GetFPData(btParams.FrameThreadSafe, EndTimeIndex.Index);

			// If waiting time isn't over yet, then we need more frames executing this Leaf
			// So we say that we're still Running
			if (currentTime < endTime)
			{
				return BTStatus.Running;
			}

			// If the waiting time is over, then we succeeded on waiting that amount of time
			// Then we return Success
			return BTStatus.Success;
		}
	}
}
