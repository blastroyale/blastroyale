using Photon.Deterministic;
using System;

namespace Quantum
{
	[Serializable]
	public unsafe partial class BTCooldown : BTDecorator
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		// How many time should we wait
		public FP CooldownTime;

		// An indexer so we know when the time started counting
		public BTDataIndex StartTimeIndex;

		// ========== BTNode INTERFACE ================================================================================

		public override void Init(Frame frame, AIBlackboardComponent* blackboard, BTAgent* agent)
		{
			base.Init(frame, blackboard, agent);

			// We allocate space on the BTAgent so we can store the Start Time
			agent->AddFPData(frame, 0);
		}

		protected override BTStatus OnUpdate(BTParams btParams)
		{
			var result = base.OnUpdate(btParams);

			// We let the time check, which happens on the DryRun, happen
			// If it results in success, then we store on the BTAgent the time value of the moment that it happened
			if (result == BTStatus.Success)
			{
				var currentTime = btParams.Frame.DeltaTime * btParams.Frame.Number;

				var frame = btParams.Frame;
				var entity = btParams.Entity;
				btParams.Agent->SetFPData(frame, currentTime, StartTimeIndex.Index);
			}

			return result;
		}

		// We get the Start Time stored on the BTAgent, then we check if the time + cooldown is already over
		// If it is not over, then we return False, blocking the execution of the children nodes
		public override Boolean DryRun(BTParams btParams)
		{
			var frame = btParams.Frame;
			var entity = btParams.Entity;
			FP startTime = btParams.Agent->GetFPData(frame, StartTimeIndex.Index);

			var currentTime = btParams.Frame.DeltaTime * btParams.Frame.Number;

			return currentTime >= startTime + CooldownTime;
		}
	}
}
