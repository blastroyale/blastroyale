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
			var moveDirection = bb.GetVector3(f, Constants.MoveDirectionKey);

			// We have to call "Move" method every frame, even with seemingly Zero velocity because any movement of CharacterController,
			// even the internal gravitational one, is being processed ONLY when we call the "Move" method
			kcc->Move(f, e, moveDirection);

			if (aimDirection.SqrMagnitude > FP._0)
			{
				QuantumHelpers.LookAt2d(f, e, aimDirection);
			}
		}
	}
}