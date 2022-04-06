using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Handles player movement and aiming direction.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class PlayerMoveAction : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			var kcc = f.Unsafe.GetPointer<CharacterController3D>(e);

			var bb = f.Get<AIBlackboardComponent>(e);

			var aimDirection = bb.GetVector2(f, Constants.AimDirectionKey);
			var speedModifier = bb.GetFP(f, Constants.SpeedModifierKey);
			var moveDirection = bb.GetVector3(f, Constants.MoveDirectionKey);
			var maxFallSpeed = bb.GetFP(f, Constants.SkyDiveFallModKey);

			// We need to set this every frame, since player input overwrites it
			kcc->MaxSpeed *= speedModifier;

			// We have to call "Move" method every frame, even with seemingly Zero velocity because any movement of CharacterController,
			// even the internal gravitational one, is being processed ONLY when we call the "Move" method
			kcc->Move(f, e, moveDirection);

			//set the falling velocity manually when parachuting
			if (speedModifier != FP._1)
				kcc->Velocity.Y = maxFallSpeed;


			if (aimDirection.SqrMagnitude > FP._0)
			{
				QuantumHelpers.LookAt2d(f, e, aimDirection);
			}
		}
	}
}