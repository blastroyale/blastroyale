using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This action shoots at player's input aiming direction and sends the event OnAttackFinished
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe class PlayerShapeAttackAction : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			var playerCharacter = f.Get<PlayerCharacter>(e);
			var weapon = f.Get<Weapon>(e);
			var config = f.WeaponConfigs.GetConfig(weapon.GameId);
			var input = f.GetPlayerInput(playerCharacter.Player);
			var aimingDirection = input->AimingDirection;
			var startPosition = f.Get<Transform3D>(e).Position;
			var direction = aimingDirection.XOY.Normalized * config.ProjectileRange;
			var shape = Shape3D.CreateBox(new FPVector3(1, 10, 1));
			var rotation = FPQuaternion.Euler(aimingDirection.XOY);
			var hit = f.Physics3D.ShapeCast(startPosition, rotation, &shape, direction, f.PlayerCastLayerMask, 
			                                 QueryOptions.HitDynamics | QueryOptions.HitKinematics);
			
			f.Events.OnPlayerAttacked(e, playerCharacter);
			
			if (hit.HasValue)
			{
				// Triggered if the player hit's target
				if (QuantumHelpers.IsAttackable(f, hit.Value.Entity, f.Get<Targetable>(e).Team))
				{
					f.Signals.PlayerAttackHit(playerCharacter.Player, e, hit.Value.Entity);
				}

				f.Events.OnPlayerAttackHit(e, playerCharacter.Player, hit.Value);

				return;
			}
			
			f.Events.OnPlayerAttackMiss(e, playerCharacter.Player);
		}
	}
}