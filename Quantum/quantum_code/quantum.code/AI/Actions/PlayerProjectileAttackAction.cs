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

			//targetAttackAngle depend on a current character velocity 
			var targetAttackAngle = FPMath.Lerp(weaponConfig.MinAttackAngle, weaponConfig.MaxAttackAngle,
												cVelocitySqr / maxSpeedSqr);

			//accuracy modifier is found by getting a random angle between the min and max angle values,
			//and then creating a rotation vector that is passed onto the projectile : only works for single shot weapons
			var angle = targetAttackAngle / FP._2;
			var shotAngle = weaponConfig.NumberOfShots == 1 ? f.RNG->Next(-angle, angle) : FP._0;
			var newAngleVector = FPVector2.Rotate(aimingDirection, shotAngle * FP.Deg2Rad).XOY;

			var projectile = new Projectile
			{
				Attacker = e,
				Direction = newAngleVector,
				PowerAmount = (uint) power.AsInt,
				KnockbackAmount = weaponConfig.KnockbackAmount,
				SourceId = weaponConfig.Id,
				Range = weaponConfig.AttackRange,
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

			f.Events.OnPlayerAttack(player, e, playerCharacter->CurrentWeapon, shotAngle, (uint)targetAttackAngle);
			f.Events.OnLocalPlayerAttack(player, e, weaponConfig);
			Projectile.Create(f, projectile);
		}
	}
}