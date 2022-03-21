using System;

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
			var player = f.Unsafe.GetPointer<PlayerCharacter>(e);
			f.Events.OnLocalPlayerLanded(player->Player, e);
		}
	}
}