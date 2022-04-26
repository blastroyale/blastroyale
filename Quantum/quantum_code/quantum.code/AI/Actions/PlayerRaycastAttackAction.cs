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
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(e);
			var weaponConfig = f.WeaponConfigs.GetConfig(playerCharacter->CurrentWeapon.GameId);
			var player = playerCharacter->Player;
			var position = f.Get<Transform3D>(e).Position + FPVector3.Up*FP._0_50;
			var team = f.Get<Targetable>(e).Team;
			var bb = f.Get<AIBlackboardComponent>(e);
			var powerAmount = (uint) f.Get<Stats>(e).GetStatData(StatType.Power).StatValue.AsInt;
			var aimingDirection = bb.GetVector2(f, Constants.AimDirectionKey).Normalized;
			var raycastShot = new RaycastShots
			{
				Attacker = e,
				WeaponConfigId = weaponConfig.Id,
				TeamSource = team,
				SpawnPosition = position,
				Direction = aimingDirection,
				StartTime = f.Time,
				PreviousTime = f.Time,
				PowerAmount = powerAmount,
				AttackAngle = weaponConfig.AttackAngle,
				Range = weaponConfig.AttackRange,
				Speed = weaponConfig.AttackHitSpeed,
				SplashRadius = weaponConfig.SplashRadius,
				CanHitSameTarget = weaponConfig.CanHitSameTarget,
				NumShots = weaponConfig.NumShots
			};
			
			playerCharacter->ReduceAmmo(f, e, 1);
			
			f.Add(f.Create(), raycastShot);
			f.Events.OnPlayerAttack(player, e);
			f.Events.OnLocalPlayerAttack(player, e);
		}
	}
}