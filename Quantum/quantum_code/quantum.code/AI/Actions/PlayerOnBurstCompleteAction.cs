using System;
using static Quantum.AIConfig;

namespace Quantum
{
	/// <summary>
	/// This action sets the IsShooting value to false only when the burst has completed 
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public class PlayerOnBurstCompleteAction : AIAction
	{
		/// <inheritdoc />
		public unsafe override void Update(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			var value = bb->GetFP(f, Constants.BURST_SHOT_COUNT);

			bb->Set(f, Constants.IS_SHOOTING_KEY, value > 0);
		}
	}
}