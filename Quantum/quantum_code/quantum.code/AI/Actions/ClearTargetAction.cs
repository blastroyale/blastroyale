using System;
using System.Diagnostics;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This action sets the variable provided to EntityRef.None and sends TargetChanged signal with EntityRef.None value
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe class ClearTargetAction : AIAction
	{
		public AIBlackboardValueKey OutTargetEntity;

		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			var bbComponent = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			
			f.Signals.TargetChanged(e, EntityRef.None);
			
			if (bbComponent->TryGetID(f, OutTargetEntity.Key, out _))
			{
				bbComponent->Set(f, OutTargetEntity.Key, EntityRef.None);
			}
		}
	}
}