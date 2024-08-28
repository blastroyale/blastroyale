using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Teleports the player to a preconfigured height, i.e.
	/// starts the parachute drop.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
		GenerateAssetResetMethod = false)]
	public unsafe class SkydiveStartAction : AIAction
	{
		public AIParamFP SkydiveHeight;

		public override void Update(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var player = f.Unsafe.GetPointer<PlayerCharacter>(e);
			player->Spawn(f, e);
			player->Activate(f, e);
			f.Events.OnLocalPlayerSkydiveDrop(player->Player, e);
			f.Events.OnPlayerSkydiveDrop(player->Player, e);
		}
	}
}