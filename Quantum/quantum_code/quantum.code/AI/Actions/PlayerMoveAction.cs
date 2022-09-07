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
		public AIParamFP MaxSpeedModifier;
		public AIParamFPVector3 VelocityModifier;

		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			if (f.Has<BotCharacter>(e))
			{
				return;
			}
			
			var kcc = f.Unsafe.GetPointer<CharacterController3D>(e);
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			var velocityModifier = VelocityModifier.Resolve(f, e, bb, null);
			var maxSpeed = MaxSpeedModifier.Resolve(f, e, bb, null);
			var aimDirection = bb->GetVector2(f, Constants.AimDirectionKey);
			var moveDirection = bb->GetVector2(f, Constants.MoveDirectionKey).XOY;
			var velocity = kcc->Velocity;

			velocity.X = velocityModifier.X != FP._0 ? velocityModifier.X : velocity.X;
			velocity.Y = velocityModifier.Y != FP._0 ? velocityModifier.Y : velocity.Y;
			velocity.Z = velocityModifier.Z != FP._0 ? velocityModifier.Z : velocity.Z;
			
			var speedUpMutatorExists = f.Context.TryGetMutatorByType(MutatorType.Speed, out var speedUpMutatorConfig);

			kcc->MaxSpeed = speedUpMutatorExists?maxSpeed * FP.FromString(speedUpMutatorConfig.Param1):maxSpeed;
			kcc->Velocity = velocity;

			// We have to call "Move" method every frame, even with seemingly Zero velocity because any movement of CharacterController,
			// even the internal gravitational one, is being processed ONLY when we call the "Move" method
			kcc->Move(f, e, moveDirection);
			
			// If player aims then we turn character towards aiming and send aiming event
			if (aimDirection.SqrMagnitude > FP._0)
			{
				QuantumHelpers.LookAt2d(f, e, aimDirection);
			}
		}
	}
}