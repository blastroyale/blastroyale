using System;

namespace Quantum
{
	/// <summary>
	/// Handles logic when we start a Parachute Landing Fall.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class SkydivePLFAction : AIAction
	{
		public override void Update(Frame f, EntityRef e)
		{
			var player = f.Get<PlayerCharacter>(e);
			f.Events.OnLocalPlayerSkydivePLF(player.Player, e);
			f.Events.OnPlayerSkydivePLF(player.Player, e);
		}
	}
}