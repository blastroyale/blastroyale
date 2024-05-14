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
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(e);
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			var weaponConfig = f.WeaponConfigs.GetConfig(playerCharacter->CurrentWeapon.GameId);
			var player = playerCharacter->Player;
			var position = f.Unsafe.GetPointer<Transform3D>(e)->Position + FPVector3.Up*FP._0_50;
			var team = f.Unsafe.GetPointer<Targetable>(e)->Team;
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			var power = f.Unsafe.GetPointer<Stats>(e)->GetStatData(StatType.Power).StatValue * weaponConfig.PowerToDamageRatio;
			var rangeStat = f.Unsafe.GetPointer<Stats>(e)->GetStatData(StatType.AttackRange).StatValue;
			var aimingDirection = QuantumHelpers.GetAimDirection(bb->GetVector2(f, Constants.AimDirectionKey), transform->Rotation).Normalized;

			//targetAttackAngle depend on a current character velocity 
			var targetAttackAngle = weaponConfig.MinAttackAngle;
			var shotAngle = weaponConfig.NumberOfShots == 1 ?
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
				AttackAngle = (uint)targetAttackAngle,
				Range = rangeStat,
				Speed = weaponConfig.AttackHitSpeed,
				SplashRadius = weaponConfig.SplashRadius,
				CanHitSameTarget = weaponConfig.CanHitSameTarget,
				NumberOfShots = weaponConfig.NumberOfShots,
				AccuracyModifier = shotAngle
			};

			playerCharacter->ReduceMag(f, e); //consume a shot from your magazine
			bb->Set(f, Constants.BurstShotCount, bb->GetFP(f, Constants.BurstShotCount) - 1); //reduce burst count by 1
			bb->Set(f, Constants.LastShotAt, f.Time);
			f.Add(f.Create(), raycastShot);
			var finalDirection = FPVector2.Rotate(aimingDirection, targetAttackAngle * FP.Deg2Rad).XY;
			f.Events.OnPlayerAttack(player, e, playerCharacter->CurrentWeapon, weaponConfig, finalDirection, rangeStat);
		}
	}
}