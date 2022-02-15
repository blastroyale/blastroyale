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
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(e);
			var weaponConfig = f.WeaponConfigs.GetConfig(playerCharacter->CurrentWeapon.GameId);
			var player = playerCharacter->Player;
			var position = f.Get<Transform3D>(e).Position + FPVector3.Up;
			var angleCount = FPMath.FloorToInt(weaponConfig.AttackAngle / Constants.RaycastAngleSplit) + 1;
			var angleStep = weaponConfig.AttackAngle / FPMath.Max(FP._1, angleCount - 1);
			var angle = -(int) weaponConfig.AttackAngle / FP._2;
			var team = f.Get<Targetable>(e).Team;
			var hitQuery = QueryOptions.HitDynamics | QueryOptions.HitKinematics | QueryOptions.HitStatics;
			var bb = f.Get<AIBlackboardComponent>(e);
			var powerAmount = (uint) f.Get<Stats>(e).GetStatData(StatType.Power).StatValue.AsInt;
			var aimingDirection = bb.GetVector2(f, Constants.AimDirectionKey).Normalized * weaponConfig.AttackRange;
			
			playerCharacter->ReduceAmmo(f, e, 1);
			f.Events.OnPlayerAttack(player, e);
			f.Events.OnLocalPlayerAttack(player, e);

			for (var i = 0; i < angleCount; i++)
			{
				var direction = FPVector2.Rotate(aimingDirection, angle * FP.Deg2Rad);
				var hit = f.Physics3D.Raycast(position, direction.XOY, weaponConfig.AttackRange, f.TargetAllLayerMask, hitQuery);
				
				angle += angleStep;

				if (!hit.HasValue || hit.Value.Entity == e)
				{
					continue;
				}

				QuantumHelpers.ProcessHit(f, e, hit.Value.Entity, hit.Value.Point, team, powerAmount);

				if (weaponConfig.SplashRadius == FP._0)
				{
					continue;
				}

				QuantumHelpers.ProcessAreaHit(f, e, e, weaponConfig.SplashRadius, hit.Value.Point, powerAmount, team);
			}
		}
	}
}