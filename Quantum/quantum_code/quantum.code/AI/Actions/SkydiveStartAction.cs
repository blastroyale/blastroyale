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
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			transform->Position.Y = FP._0;
			transform->Position += FPVector3.Up * SkydiveHeight.Resolve(f, e, bb, null);

			if (f.Unsafe.TryGetPointer<BotCharacter>(e, out var bot))
			{
				if (bot->SpawnWithPlayer)
				{
					transform->Position.Y += FP._4 + f.RNG->Next();
					transform->Position.X += f.RNG->NextInclusive(FP._1,FP._3) * f.RNG->Sign() ;
					transform->Position.Z += f.RNG->NextInclusive(FP._1,FP._3) * f.RNG->Sign() ;
				}
			}

			player->Spawn(f, e);
			player->Activate(f, e);

			f.Events.OnLocalPlayerSkydiveDrop(player->Player, e);
			f.Events.OnPlayerSkydiveDrop(player->Player, e);
		}
	}
}