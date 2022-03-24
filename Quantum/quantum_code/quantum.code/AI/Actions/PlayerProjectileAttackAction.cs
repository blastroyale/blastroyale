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
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(e);
			var weaponConfig = f.WeaponConfigs.GetConfig(playerCharacter->CurrentWeapon.GameId);
			var player = playerCharacter->Player;
			var aimingDirection = f.Get<AIBlackboardComponent>(e).GetVector2(f, Constants.AimDirectionKey).Normalized;
			var transform = f.Get<Transform3D>(e);
			var position = transform.Position + (transform.Rotation * playerCharacter->ProjectileSpawnOffset);
			var team = f.Get<Targetable>(e).Team;
			var power = f.Get<Stats>(e).GetStatData(StatType.Power).StatValue;
			var projectile = new Projectile
			{
				Attacker = e,
				Direction = aimingDirection.XOY,
				PowerAmount = (uint) power.AsInt,
				SourceId = weaponConfig.Id,
				Range = weaponConfig.AttackRange,
				SpawnPosition = position,
				Speed = weaponConfig.ProjectileSpeed,
				SplashRadius = weaponConfig.SplashRadius,
				StunDuration = FP._0,
				Target = EntityRef.None,
				TeamSource = team
			};
			
			playerCharacter->ReduceAmmo(f, e, 1);
			f.Events.OnPlayerAttack(player, e);
			f.Events.OnLocalPlayerAttack(player, e);
			Projectile.Create(f, projectile);
		}
	}
}