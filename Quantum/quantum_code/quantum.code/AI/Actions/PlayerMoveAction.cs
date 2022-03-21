using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Handles player movement, taking into account aiming and
	/// the currently equipped weapon.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class PlayerMoveAction : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(e);
			var input = f.GetPlayerInput(playerCharacter->Player);

			var kcc = f.Unsafe.GetPointer<CharacterController3D>(e);
			var rotation = FPVector2.Zero;
			var moveVelocity = FPVector3.Zero;
			var bb = f.Get<AIBlackboardComponent>(e);
			var weaponConfig = f.WeaponConfigs.GetConfig(playerCharacter->CurrentWeapon.GameId);
			var speed = f.Get<Stats>(e).Values[(int) StatType.Speed].StatValue;

			if (input->IsMoveButtonDown)
			{
				if (input->IsShootButtonDown)
				{
					speed *= weaponConfig.AimingMovementSpeed;
				}

				rotation = input->Direction;
				kcc->MaxSpeed = speed;
				moveVelocity = rotation.XOY * speed;
			}

			// bb.Set(f, Constants.IsAimingKey, input->IsShootButtonDown);
			// bb.Set(f, Constants.AimDirectionKey, input->AimingDirection);
			bb.Set(f, Constants.MoveDirectionKey, input->Direction * speed);

			// We have to call "Move" method every frame, even with seemingly Zero velocity because any movement of CharacterController,
			// even the internal gravitational one, is being processed ONLY when we call the "Move" method
			kcc->Move(f, e, moveVelocity);

			if (input->AimingDirection.SqrMagnitude > FP._0)
			{
				rotation = input->AimingDirection;
			}

			if (rotation.SqrMagnitude > FP._0)
			{
				QuantumHelpers.LookAt2d(f, e, rotation);
			}
		}
	}
}