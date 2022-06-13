using System;

namespace Quantum
{
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe partial class SetFPToCurrentTime : AIAction
	{
		public AIBlackboardValueKey Key;

		public override unsafe void Update(Frame frame, EntityRef entity)
		{
			var blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);

			var agent = frame.Unsafe.GetPointer<HFSMAgent>(entity);
			var aiConfig = agent->GetConfig(frame);

			var value = frame.Time;
			blackboard->Set(frame, Key.Key, value);
		}
	}
}