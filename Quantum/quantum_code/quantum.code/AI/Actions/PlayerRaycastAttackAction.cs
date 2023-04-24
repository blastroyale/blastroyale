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
		public override void Update(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var isAccuracyMutator = f.Context.TryGetMutatorByType(MutatorType.AbsoluteAccuracy, out _);
			var kcc = f.Unsafe.GetPointer<CharacterController3D>(e);
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(e);
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			var weaponConfig = f.WeaponConfigs.GetConfig(playerCharacter->CurrentWeapon.GameId);
			var player = playerCharacter->Player;
			var position = f.Get<Transform3D>(e).Position + FPVector3.Up*FP._0_50;
			var team = f.Get<Targetable>(e).Team;
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			var power = f.Get<Stats>(e).GetStatData(StatType.Power).StatValue * weaponConfig.PowerToDamageRatio;
			var cVelocitySqr = kcc->Velocity.SqrMagnitude;
			var maxSpeedSqr = kcc->MaxSpeed * kcc->MaxSpeed;
			var rangeStat = f.Get<Stats>(e).GetStatData(StatType.AttackRange).StatValue;
			var aimingDirection = QuantumHelpers.GetAimDirection(bb->GetVector2(f, Constants.AimDirectionKey), ref  transform->Rotation).Normalized;

			//targetAttackAngle depend on a current character velocity 
			var targetAttackAngle = isAccuracyMutator ?
				weaponConfig.MinAttackAngle : QuantumHelpers.GetDynamicAimValue(kcc, weaponConfig.MaxAttackAngle, weaponConfig.MinAttackAngle);
			var shotAngle = weaponConfig.NumberOfShots == 1 && !isAccuracyMutator ?
				   QuantumHelpers.GetSingleShotAngleAccuracyModifier(f, targetAttackAngle) :
				   FP._0;

			//only do attackSpeed ramping if the weapon has it
			var rampUpStartTime = bb->GetFP(f, Constants.RampUpTimeStart);
			if (weaponConfig.InitialAttackRampUpTime != FP._0)
			{
				var timeDiff = f.Time - rampUpStartTime;
				var currentAttackCooldown = FPMath.Lerp(weaponConfig.InitialAttackCooldown, weaponConfig.AttackCooldown, 
					timeDiff / weaponConfig.InitialAttackRampUpTime);
				bb->Set(f, nameof(weaponConfig.AttackCooldown), currentAttackCooldown);
			}

			var attackRange = QuantumHelpers.GetDynamicAimValue(kcc, rangeStat, rangeStat + weaponConfig.AttackRangeAimBonus);

			var raycastShot = new RaycastShots
			{
				Attacker = e,
				WeaponConfigId = weaponConfig.Id,
				TeamSource = team,
				SpawnPosition = position,
				Direction = aimingDirection,
				StartTime = f.Time,
				PreviousTime = f.Time,
				PowerAmount = (uint)power.AsInt,
				KnockbackAmount = weaponConfig.KnockbackAmount,
				AttackAngle = (uint)targetAttackAngle,
				Range = attackRange,
				Speed = weaponConfig.AttackHitSpeed,
				SplashRadius = weaponConfig.SplashRadius,
				CanHitSameTarget = weaponConfig.CanHitSameTarget,
				NumberOfShots = weaponConfig.NumberOfShots,
				AccuracyModifier = shotAngle
			};

			playerCharacter->ReduceMag(f, e); //consume a shot from your magazine
			bb->Set(f, Constants.BurstShotCount, bb->GetFP(f, Constants.BurstShotCount) - 1); //reduce burst count by 1

			f.Add(f.Create(), raycastShot);
			f.Events.OnPlayerAttack(player, e, playerCharacter->CurrentWeapon, weaponConfig, shotAngle, (uint)targetAttackAngle, attackRange);
		}
	}
}