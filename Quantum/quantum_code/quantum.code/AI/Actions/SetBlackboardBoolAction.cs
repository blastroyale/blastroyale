using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This action sets a Bool blackboard variable to a specified value
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe partial class SetBlackboardBoolAction : AIAction
	{
		public AIBlackboardValueKey Key;
		public bool Value;

		public override unsafe void Update(Frame f, EntityRef e)
		{
			var bbComponent = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			bbComponent->Set(f, Key.Key, Value);
		}
	}
}