using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This action shoots at player's input aiming direction and sends the event OnAttackFinished
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe class ShootAtAimingDirectionAction : AIAction
	{
		public AIBlackboardValueKey ProjectileAssetRef;
		public AIBlackboardValueKey ProjectileId;
		public AIBlackboardValueKey ProjectileSpeed;
		public AIBlackboardValueKey ProjectileRange;
		public AIBlackboardValueKey ProjectileSplashRadius;
		public AIBlackboardValueKey ProjectileStunDuration;

		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			var agent = f.Unsafe.GetPointer<HFSMAgent>(e);
			var stats = f.Get<Stats>(e);
			var bbComponent = f.Get<AIBlackboardComponent>(e);
			var projectileId = (GameId) bbComponent.GetInteger(f, ProjectileId.Key);
			var spawnOffset = FPVector3.One;
			var isHealing = false;
			var transform = f.Get<Transform3D>(e);
			var normalizedOriginalDirection = FPVector3.Zero;
			var normalizedDirection = normalizedOriginalDirection;
			
			// If entity has a Weapon then we check the capacity and if it's 0 then finish attack without creating a projectile
			if (f.Unsafe.TryGetPointer<Weapon>(e, out var weapon))
			{
				var playerCharacter = f.Get<PlayerCharacter>(e);
				
				if (weapon->Emptied || weapon->NextShotAllowedTime > f.Time)
				{
					HFSMManager.TriggerEvent(f, &agent->Data, e, Constants.ATTACK_FINISHED_EVENT);
					f.Events.OnLocalPlayerWeaponEmpty(playerCharacter.Player, e);
					return;
				}
				
				var input = f.GetPlayerInput(f.Get<PlayerCharacter>(e).Player);
				var aimingDirection = FPVector2.Zero;

				if (f.TryGet<BotCharacter>(e, out var botCharacter))
				{
					var targetCandidate = bbComponent.GetEntityRef(f, Constants.TARGET_BB_KEY);
					var targetPosition = f.Get<Transform3D>(targetCandidate).Position;
					aimingDirection = (targetPosition - transform.Position).XZ;
					normalizedOriginalDirection = aimingDirection.XOY.Normalized;
					
					if (botCharacter.AccuracySpreadAngle > 0)
					{
						normalizedOriginalDirection = Projectile.DivertOnRandomAngle(f, normalizedOriginalDirection, (int) botCharacter.AccuracySpreadAngle);
					}
				}
				else
				{
					aimingDirection = input->AimingDirection;
					normalizedOriginalDirection = aimingDirection.XOY.Normalized;
				}
				
				normalizedDirection = normalizedOriginalDirection;
				
				// If weapon is at full capacity then we mark the next time to increase the capacity on 1 point
				if (weapon->Ammo == weapon->MaxAmmo)
				{
					weapon->NextCapacityIncreaseTime = f.Time + weapon->OneCapacityReloadingTime;
				}
				
				if (weapon->BulletSpreadAngle > 0)
				{
					normalizedDirection = Projectile.DivertOnRandomAngle(f, normalizedDirection, (int) weapon->BulletSpreadAngle);
				}
				
				spawnOffset = f.Get<PlayerCharacter>(e).ProjectileSpawnOffset;
				isHealing = weapon->IsHealing;
				weapon->Ammo = Math.Max(weapon->Ammo - 1, 0);
				weapon->NextShotAllowedTime = f.Time + weapon->AttackCooldown;
				
				if (weapon->Ammo == 0)
				{
					weapon->Emptied = true;
				}
				
				projectileId = weapon->IsHealing ? weapon->ProjectileHealingId : projectileId;
			}
			else
			{
				normalizedOriginalDirection = (transform.Rotation * FPVector3.Forward).Normalized;
				normalizedDirection = normalizedOriginalDirection;
			}
			
			var spawnPosition = transform.Position + FPQuaternion.AngleAxis(transform.EulerAngles.Y, FPVector3.Up) * spawnOffset;
			var projectileRange = bbComponent.GetFP(f, ProjectileRange.Key);
			
			var projectileData = new ProjectileData
			{
				Attacker = e,
				ProjectileId = projectileId,
				ProjectileAssetRef = bbComponent.GetFP(f, ProjectileAssetRef.Key).RawValue,
				NormalizedDirection = normalizedDirection,
				OriginalDirection = normalizedOriginalDirection,
				SpawnPosition = spawnPosition,
				TeamSource = f.Get<Targetable>(e).Team,
				IsHealing = isHealing,
				PowerAmount = (uint) stats.Values[(int) StatType.Power].StatValue.AsInt,
				Speed = bbComponent.GetFP(f, ProjectileSpeed.Key),
				Range = projectileRange,
				SplashRadius = bbComponent.GetFP(f, ProjectileSplashRadius.Key),
				StunDuration = bbComponent.GetFP(f, ProjectileStunDuration.Key),
				IsHitOnRangeLimit = projectileRange <= Constants.MELEE_WEAPON_RANGE_THRESHOLD,
				IsHitOnlyOnRangeLimit = projectileRange <= Constants.MELEE_WEAPON_RANGE_THRESHOLD,
			};
			
			f.Signals.ProjectileShootTriggered(Projectile.Create(f, projectileData));
			
			HFSMManager.TriggerEvent(f, &agent->Data, e, Constants.ATTACK_FINISHED_EVENT);
		}
	}
}