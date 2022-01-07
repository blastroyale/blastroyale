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
			var power = f.Get<Stats>(e).GetStatData(StatType.Power).StatValue;
			var projectile = new Projectile
			{
				Attacker = e,
				Direction = aimingDirection.XOY,
				IsPiercing = false,
				PowerAmount = (uint) power.AsInt,
				SourceId = weapon->WeaponId,
				Range = weapon->AttackRange,
				SpawnPosition = position + weapon->ProjectileSpawnOffset,
				Speed = weapon->ProjectileSpeed,
				SplashRadius = weapon->SplashRadius,
				StunDuration = FP._0,
				Target = EntityRef.None,
				TeamSource = team
			};
			
			weapon->Ammo--;
			weapon->LastAttackTime = f.Time;

			Projectile.Create(f, projectile);
			
			f.Events.OnPlayerAttacked(e, player);
		}
	}
}