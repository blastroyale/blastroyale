using System;

namespace Quantum
{
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe partial class SetBlackboardBool : AIAction
	{
		public AIBlackboardValueKey Key;
		public AIParamBool Value;

		public override unsafe void Update(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			var blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);

			var agent = frame.Unsafe.GetPointer<HFSMAgent>(entity);
			var aiConfig = agent->GetConfig(frame);

			var value = Value.Resolve(frame, entity, blackboard, aiConfig);
			blackboard->Set(frame, Key.Key, value);
		}
	}
}