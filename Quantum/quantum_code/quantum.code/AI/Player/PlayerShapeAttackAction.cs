using System;
using Photon.Deterministic;
using Quantum.Physics3D;

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
			var weapon = f.Unsafe.GetPointer<Weapon>(e);
			var player = playerCharacter.Player;
			var aimingDirection = f.GetPlayerInput(player)->AimingDirection; // TODO outside of here
			var position = f.Get<Transform3D>(e).Position;
			var team = f.Get<Targetable>(e).Team;
			var angleCount = FPMath.FloorToInt(weapon->BulletSpreadAngle / (FP._1 * 10)) + 1;
			var angleStep = weapon->BulletSpreadAngle / FPMath.Max(FP._1, angleCount - 1);
			var angle = -(FP) weapon->BulletSpreadAngle / FP._2;
			
			f.Events.OnPlayerAttacked(e, player);

			for (var i = 0; i < angleCount; i++)
			{
				var direction = FPVector2.Rotate(aimingDirection, angle * FP.Deg2Rad);
				var hit = f.Physics3D.Raycast(position, direction.XOY, weapon->Range, f.PlayerCastLayerMask);
				
				ProcessHit(f, e, player, hit, team);

				angle += angleStep;
			}
			
			weapon->Capacity--;
			weapon->LastAttackTime = f.Time;
		}

		private void ProcessHit(Frame f, EntityRef attackerEntity, PlayerRef attacker, Hit3D? hit, int attackerTeam)
		{
			if (!hit.HasValue)
			{
				f.Events.OnPlayerAttackMiss(attackerEntity, attacker);
				
				return;
			}
			
			if (QuantumHelpers.IsAttackable(f, hit.Value.Entity, attackerTeam))
			{
				f.Signals.PlayerAttackHit(attacker, attackerEntity, hit.Value.Entity);
			}

			f.Events.OnPlayerAttackHit(attackerEntity, attacker, hit.Value.Entity, hit.Value.Point);
		}
	}
}