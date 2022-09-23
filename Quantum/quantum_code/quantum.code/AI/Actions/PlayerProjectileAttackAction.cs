using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This action attacks at <see cref="PlayerCharacter"/> aiming direction based on it's <see cref="Weapon"/> data
	/// and it's an projectile speed based attack
	/// </summary>
	/// <remarks>
	/// Use <see cref="PlayerRaycastAttackAction"/> if is not a projectile speed base attack
	/// </remarks>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe class PlayerProjectileAttackAction : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			var kcc = f.Unsafe.GetPointer<CharacterController3D>(e);
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(e);
			var weaponConfig = f.WeaponConfigs.GetConfig(playerCharacter->CurrentWeapon.GameId);
			var player = playerCharacter->Player;
			var aimingDirection = f.Get<AIBlackboardComponent>(e).GetVector2(f, Constants.AimDirectionKey).Normalized;
			var transform = f.Get<Transform3D>(e);
			var position = transform.Position + (transform.Rotation * playerCharacter->ProjectileSpawnOffset);
			var team = f.Get<Targetable>(e).Team;
			var power = f.Get<Stats>(e).GetStatData(StatType.Power).StatValue;
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			var cVelocitySqr = kcc->Velocity.SqrMagnitude;
			var maxSpeedSqr = kcc->MaxSpeed * kcc->MaxSpeed;
			var attackRange = f.Get<Stats>(e).GetStatData(StatType.AttackRange).StatValue;

			//targetAttackAngle depend on a current character velocity 
			var targetAttackAngle = FPMath.Lerp(weaponConfig.MinAttackAngle, weaponConfig.MaxAttackAngle,
												cVelocitySqr / maxSpeedSqr);
			var shotAngle = FP._0;

			//accuracy modifier is found by approximate normal distribution random,
			//and then creating a rotation vector that is passed onto the projectile; only works for single shot weapons
			if (weaponConfig.NumberOfShots == 1)
			{
				var rngNumber = f.RNG->NextInclusive(0,100);
				var angleStep = targetAttackAngle / Constants.APPRX_NORMAL_DISTRIBUTION.Length;
				
				for (var i = 0; i < Constants.APPRX_NORMAL_DISTRIBUTION.Length; i++)
				{
					if (rngNumber <= Constants.APPRX_NORMAL_DISTRIBUTION[i])
					{
						shotAngle = f.RNG->Next(angleStep * i, angleStep * (i + 1)) - (targetAttackAngle / FP._2);
						break;
					}
					
					// i = 2
					// approximateNormalDistribution[i] = 37
					// angleStep = 60 / 7 = 8.57
					// 0,			1,				2,				3,				4,				5,				6
					// 0 - 8.57,	8.57 - 17.14	17.14 - 25.71	25.71 - 34.28	34.28 - 42.85	42.85 - 51.42	51.42 - 60
				}
			}
			
			var newAngleVector = FPVector2.Rotate(aimingDirection, shotAngle * FP.Deg2Rad).XOY;

			var projectile = new Projectile
			{
				Attacker = e,
				Direction = newAngleVector,
				PowerAmount = (uint)power.AsInt,
				KnockbackAmount = weaponConfig.KnockbackAmount,
				SourceId = weaponConfig.Id,
				Range = attackRange,
				SpawnPosition = position,
				Speed = weaponConfig.AttackHitSpeed,
				SplashRadius = weaponConfig.SplashRadius,
				SplashDamageRatio = weaponConfig.SplashDamageRatio,
				StunDuration = FP._0,
				Target = EntityRef.None,
				TeamSource = team
			};
			
			playerCharacter->ReduceAmmo(f, e, 1);
			bb->Set(f, Constants.BurstShotCount, bb->GetFP(f, Constants.BurstShotCount) - 1);

			f.Events.OnPlayerAttack(player, e, playerCharacter->CurrentWeapon, weaponConfig, shotAngle, (uint)targetAttackAngle);
			Projectile.Create(f, projectile);
		}
	}
}