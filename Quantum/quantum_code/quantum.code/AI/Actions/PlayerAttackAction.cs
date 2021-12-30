using System;
using Photon.Deterministic;
using Quantum.Physics3D;

namespace Quantum
{
	/// <summary>
	/// This action attacks at <see cref="PlayerCharacter"/> aiming direction based on it's <see cref="Weapon"/> data
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe class PlayerAttackAction : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			var playerCharacter = f.Get<PlayerCharacter>(e);
			var weapon = f.Unsafe.GetPointer<Weapon>(e);
			var player = playerCharacter.Player;
			var aimingDirection = f.Get<AIBlackboardComponent>(e).GetVector2(f, Constants.AimDirectionKey);
			var position = f.Get<Transform3D>(e).Position;
			var angleCount = FPMath.FloorToInt(weapon->BulletSpreadAngle / (FP._1 * 10)) + 1;
			var angleStep = weapon->BulletSpreadAngle / FPMath.Max(FP._1, angleCount - 1);
			var angle = -(FP) weapon->BulletSpreadAngle / FP._2;
			var team = f.Get<Targetable>(e).Team;
			var attackHit = false;
			
			weapon->Ammo--;
			weapon->LastAttackTime = f.Time;
			
			f.Events.OnPlayerAttacked(e, player);

			for (var i = 0; i < angleCount; i++)
			{
				var direction = FPVector2.Rotate(aimingDirection, angle * FP.Deg2Rad);
				var hit = f.Physics3D.Raycast(position, direction.XOY, weapon->Range, f.PlayerCastLayerMask);
				
				angle += angleStep;

				if (!hit.HasValue)
				{
					continue;
				}

				if (hit.Value.IsDynamic)
				{
					ProcessHit(f, e, player, hit.Value, team);

					attackHit = true;
				}

				if (weapon->SplashRadius > FP._0)
				{
					
				}
			}

			if (!attackHit)
			{
				f.Events.OnPlayerAttackMiss(e, player);
			}
		}

		private void ProcessHit(Frame f, EntityRef attackerEntity, PlayerRef attacker, Hit3D hit, int attackerTeam)
		{
			if (QuantumHelpers.IsAttackable(f, hit.Entity, attackerTeam))
			{
				var amount = f.Get<Stats>(attackerEntity).GetStatData(StatType.Power).StatValue.AsInt;
				
				f.Signals.AttackHit(attackerEntity, hit.Entity, amount);
			}

			f.Events.OnPlayerAttackHit(attackerEntity, attacker, hit.Entity, hit.Point);
		}
	}
}