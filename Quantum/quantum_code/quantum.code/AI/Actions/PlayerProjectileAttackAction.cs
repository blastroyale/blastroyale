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
		public override void Update(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(e);
			var weaponConfig = f.WeaponConfigs.GetConfig(playerCharacter->CurrentWeapon.GameId);
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			var position = transform->Position + (transform->Rotation * playerCharacter->ProjectileSpawnOffset);
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			var aimingDirection = QuantumHelpers.GetAimDirection(bb->GetVector2(f, Constants.AimDirectionKey), ref transform->Rotation).Normalized;
			var rangeStat = f.Get<Stats>(e).GetStatData(StatType.AttackRange).StatValue;
			playerCharacter->ReduceMag(f, e); //consume a shot from your magazine
			bb->Set(f, Constants.BurstShotCount, bb->GetFP(f, Constants.BurstShotCount) - 1);
			bb->Set(f, Constants.LastShotAt, f.Time);
			f.Events.OnPlayerAttack(playerCharacter->Player, e, playerCharacter->CurrentWeapon, weaponConfig, aimingDirection, rangeStat);
			if (weaponConfig.NumberOfShots == 1 || weaponConfig.IsMeleeWeapon)
			{
				Projectile.CreateProjectile(f, e, rangeStat, aimingDirection, position, weaponConfig);
			}
			else
			{
				FP max = weaponConfig.MinAttackAngle;
				FP angleStep = weaponConfig.MinAttackAngle / weaponConfig.NumberOfShots;
				FP angle = -max/ FP._2;
				for (var x = 0; x < weaponConfig.NumberOfShots; x++)
				{
					var burstDirection = FPVector2.Rotate(aimingDirection, angle * FP.Deg2Rad).XOY;
					Projectile.CreateProjectile(f, e, rangeStat, burstDirection.XZ, position, weaponConfig);
					angle += angleStep;
				}
			}
		}
	}
}