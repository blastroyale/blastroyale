using System;
using System.Runtime.InteropServices;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This action sets blackboard entity ref variable to a specified value
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public class SetEntityRefAction : AIAction
	{
		public AIBlackboardValueKey Entity;
		public AIBlackboardValueKey OutEntity;

		/// <inheritdoc />
		public override unsafe void Update(Frame f, EntityRef e)
		{
			var bbComponent = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			EntityRef entityValue = EntityRef.None;

			if (bbComponent->TryGetID(f, Entity.Key, out _))
			{
				entityValue = bbComponent->GetEntityRef(f, Entity.Key);
			}

			bbComponent->Set(f, OutEntity.Key, entityValue);
		}
	}
}