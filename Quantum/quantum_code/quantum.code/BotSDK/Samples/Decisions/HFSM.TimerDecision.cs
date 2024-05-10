using System;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class TimerDecision : HFSMDecision
	{
		public AIParamFP TimeToTrueState = FP._3;

		public override unsafe bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			var agent = frame.Unsafe.GetPointer<HFSMAgent>(entity);
			var aiConfig = agent->GetConfig(frame);

			FP requiredTime;
			if (frame.Has<AIBlackboardComponent>(entity))
			{
				requiredTime = TimeToTrueState.Resolve(frame, entity, frame.Unsafe.GetPointer<AIBlackboardComponent>(entity), aiConfig, ref aiContext);
			}
			else
			{
				AIBlackboardComponent aiBlackboardComponent = default;
				requiredTime = TimeToTrueState.Resolve(frame, entity, &aiBlackboardComponent, aiConfig, ref aiContext);
			}

			var hfsmData = &agent->Data;
			return hfsmData->Time >= requiredTime;
		}
	}
}
