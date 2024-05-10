using System;

namespace Quantum
{
	/// <summary>
	/// Handles logic when we the player actually grounds to the map fully and starts playing
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class SkydiveFullyGroundedAction : AIAction
	{
		public override void Update(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var player = f.Unsafe.GetPointer<PlayerCharacter>(e);
			f.Events.OnLocalPlayerSkydiveFullyGrounded(player->Player, e);
			f.Events.OnPlayerSkydiveFullyGrounded(player->Player, e);
		}
	}
}