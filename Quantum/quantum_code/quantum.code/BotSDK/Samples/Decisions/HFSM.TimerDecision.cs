using System;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class TimerDecision : HFSMDecision
	{
		public AIParamFP TimeToTrueState = FP._3;

		public override unsafe bool Decide(Frame frame, EntityRef entity)
		{
			var blackboard = frame.Has<AIBlackboardComponent>(entity) ? frame.Get<AIBlackboardComponent>(entity) : default;

			var agent = frame.Unsafe.GetPointer<HFSMAgent>(entity);
			var aiConfig = agent->GetConfig(frame);

			FP requiredTime = TimeToTrueState.Resolve(frame, entity, &blackboard, aiConfig);

			var hfsmData = &agent->Data;
			return hfsmData->Time >= requiredTime;
		}
	}
}
