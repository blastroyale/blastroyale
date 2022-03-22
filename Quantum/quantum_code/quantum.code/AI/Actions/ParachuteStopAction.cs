using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Sends an event when the player has landed on the ground.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class ParachuteStopAction : AIAction
	{
		public override void Update(Frame f, EntityRef e)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			bb->Set(f, Constants.SpeedModifierKey, FP._1);

			var player = f.Unsafe.GetPointer<PlayerCharacter>(e);
			f.Events.OnLocalPlayerLanded(player->Player, e);
			f.Events.OnPlayerLanded(player->Player, e);
		}
	}
}