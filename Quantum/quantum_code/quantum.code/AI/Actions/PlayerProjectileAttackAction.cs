using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This action attacks at <see cref="PlayerCharacter"/> aiming direction based on it's <see cref="Weapon"/> data
	/// and it's an projectile speed based attack
	/// </summary>
	/// <remarks>
	/// Use <see cref="PlayerAttackAction"/> if is not a projectile speed base attack
	/// </remarks>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe class PlayerProjectileAttackAction : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			var playerCharacter = f.Get<PlayerCharacter>(e);
			var weapon = f.Unsafe.GetPointer<Weapon>(e);
			var player = playerCharacter.Player;
			var aimingDirection = f.Get<AIBlackboardComponent>(e).GetVector2(f, Constants.AimDirectionKey);
			var position = f.Get<Transform3D>(e).Position;
			var team = f.Get<Targetable>(e).Team;
			
			weapon->Ammo--;
			weapon->LastAttackTime = f.Time;
			
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
			
			f.Events.OnPlayerAttacked(e, player);
		}
	}
}