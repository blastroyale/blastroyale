using System;
using Photon.Deterministic;

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
	public unsafe class PlayerRaycastAttackAction : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			var kcc = f.Unsafe.GetPointer<CharacterController3D>(e);
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(e);
			var weaponConfig = f.WeaponConfigs.GetConfig(playerCharacter->CurrentWeapon.GameId);
			var player = playerCharacter->Player;
			var position = f.Get<Transform3D>(e).Position + FPVector3.Up*FP._0_50;
			var team = f.Get<Targetable>(e).Team;
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			var powerBase = (uint) f.Get<Stats>(e).GetStatData(StatType.Power).StatValue.AsInt;
			var powerRatio = QuantumStatCalculator.GetScaledPowerRatio(f, playerCharacter->CurrentWeapon);
			var finalPower = powerBase * powerRatio;
			var aimingDirection = bb->GetVector2(f, Constants.AimDirectionKey).Normalized;

			var cVelocitySqr = kcc->Velocity.SqrMagnitude;
			var maxSpeedSqr = kcc->MaxSpeed * kcc->MaxSpeed;

			//targetAttackAngle depend on a current character velocity 
			var targetAttackAngle = FPMath.Lerp(weaponConfig.MinAttackAngle, weaponConfig.MaxAttackAngle, 
			                                    cVelocitySqr / maxSpeedSqr);

			//accuracy modifier is found by getting a random angle between the min and max angle values,
			//and then passing that through into the shot; Works only for single shot weapons
			var angle = targetAttackAngle / FP._2;
			var shotAngle = weaponConfig.NumberOfShots == 1 ? f.RNG->Next(-angle, angle) : FP._0;

			//only do attackSpeed ramping if the weapon has it
			var RampUpStartTime = bb->GetFP(f, Constants.RampUpTimeStart);
			if (weaponConfig.InitialAttackRampUpTime != FP._0)
			{
				var timeDiff = f.Time - RampUpStartTime;
				var CurrentAttackCooldown = FPMath.Lerp(weaponConfig.InitialAttackCooldown, weaponConfig.AttackCooldown, 
					timeDiff / weaponConfig.InitialAttackRampUpTime);
				bb->Set(f, nameof(weaponConfig.AttackCooldown), CurrentAttackCooldown);
			}

			var raycastShot = new RaycastShots
			{
				Attacker = e,
				WeaponConfigId = weaponConfig.Id,
				TeamSource = team,
				SpawnPosition = position,
				Direction = aimingDirection,
				StartTime = f.Time,
				PreviousTime = f.Time,
				PowerAmount = (uint)finalPower,
				KnockbackAmount = weaponConfig.KnockbackAmount,
				AttackAngle = (uint)targetAttackAngle,
				Range = weaponConfig.AttackRange,
				Speed = weaponConfig.AttackHitSpeed,
				SplashRadius = weaponConfig.SplashRadius,
				CanHitSameTarget = weaponConfig.CanHitSameTarget,
				NumberOfShots = weaponConfig.NumberOfShots,
				AccuracyModifier = shotAngle
			};
			
			playerCharacter->ReduceAmmo(f, e, 1);
			bb->Set(f, Constants.BurstShotCount, bb->GetFP(f, Constants.BurstShotCount) - 1);

			f.Add(f.Create(), raycastShot);
			f.Events.OnPlayerAttack(player, e, playerCharacter->CurrentWeapon, shotAngle, (uint)targetAttackAngle);
			f.Events.OnLocalPlayerAttack(player, e, weaponConfig);
		}
	}
}