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
	public unsafe class SkydiveStopAction : AIAction
	{
		public override void Update(Frame f, EntityRef e)
		{
			var player = f.Unsafe.GetPointer<PlayerCharacter>(e);

			f.Events.OnLocalPlayerSkydiveLand(player->Player, e);
			f.Events.OnPlayerSkydiveLand(player->Player, e);
		}
	}
}