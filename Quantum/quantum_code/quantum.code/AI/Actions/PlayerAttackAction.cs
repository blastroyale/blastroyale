using System;
using Photon.Deterministic;
using Quantum.Physics3D;

namespace Quantum
{
	/// <summary>
	/// This action attacks at <see cref="PlayerCharacter"/> aiming direction based on it's <see cref="Weapon"/> data
	/// </summary>
	/// <remarks>
	/// Use <see cref="PlayerProjectileAttackAction"/> if is a projectile speed base attack
	/// </remarks>
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
			var position = f.Get<Transform3D>(e).Position + FPVector3.Up;
			var angleCount = FPMath.FloorToInt(weapon->AttackAngle / (FP._1 * 10)) + 1;
			var angleStep = weapon->AttackAngle / FPMath.Max(FP._1, angleCount - 1);
			var angle = -weapon->AttackAngle / FP._2;
			var team = f.Get<Targetable>(e).Team;
			var hitQuery = QueryOptions.HitDynamics | QueryOptions.HitKinematics | QueryOptions.HitStatics;
			var shape = Shape3D.CreateSphere(weapon->SplashRadius);
			var powerAmount = (uint) f.Get<Stats>(e).GetStatData(StatType.Power).StatValue.AsInt;
			var aimingDirection = f.Get<AIBlackboardComponent>(e).GetVector2(f, Constants.AimDirectionKey).Normalized * 
			                      weapon->AttackRange;

			if (weapon->Ammo > 0)
			{
				weapon->Ammo--;
			}
			
			weapon->LastAttackTime = f.Time;
			
			f.Events.OnPlayerAttacked(e, player);

			for (var i = 0; i < angleCount; i++)
			{
				var direction = FPVector2.Rotate(aimingDirection, angle * FP.Deg2Rad);
				var hit = f.Physics3D.Raycast(position, direction.XOY, weapon->AttackRange, f.TargetAllLayerMask, hitQuery);
				
				angle += angleStep;

				if (!hit.HasValue || hit.Value.Entity == e)
				{
					continue;
				}

				QuantumHelpers.ProcessHit(f, e, hit.Value.Entity, hit.Value.Point, team, powerAmount);

				if (weapon->SplashRadius == FP._0)
				{
					continue;
				}
				
				var hits = f.Physics3D.ShapeCastAll(hit.Value.Point, FPQuaternion.Identity, shape, 
				                                    FPVector3.Zero, f.TargetAllLayerMask, QueryOptions.HitDynamics);

				for (var j = 0; j < hits.Count; j++)
				{
					if (hits[j].Entity == e)
					{
						continue;
					}
					
					QuantumHelpers.ProcessHit(f, e, hits[j].Entity, hits[j].Point, team, powerAmount);
				}
			}
		}
	}
}