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
			var position = f.Get<Transform3D>(e).Position + FPVector3.Up;
			var team = f.Get<Targetable>(e).Team;
			var bb = f.Get<AIBlackboardComponent>(e);
			var powerAmount = (uint) f.Get<Stats>(e).GetStatData(StatType.Power).StatValue.AsInt;
			var aimingDirection = bb.GetVector2(f, Constants.AimDirectionKey).Normalized;
			var angleCount = FPMath.FloorToInt(weaponConfig.AttackAngle / Constants.RaycastAngleSplit) + 1;
			var angleStep = weaponConfig.AttackAngle / FPMath.Max(FP._1, angleCount - 1);
			var angle = -(int) weaponConfig.AttackAngle / FP._2;
			
			playerCharacter->ReduceAmmo(f, e, 1);
			f.Events.OnPlayerAttack(player, e, weaponConfig.Id);
			f.Events.OnLocalPlayerAttack(player, e);
			
			for (var i = 0; i < angleCount; i++)
			{
				angle += angleStep;
				
				var raycastShot = new RaycastShot
				{
					Attacker = e,
					WeaponConfigId = weaponConfig.Id,
					TeamSource = team,
					SpawnPosition = position,
					Direction = FPVector2.Rotate(aimingDirection, angle * FP.Deg2Rad),
					PowerAmount = powerAmount,
					Range = weaponConfig.AttackRange,
					AttackHitTime = weaponConfig.AttackHitTime
				};
				
				RaycastShot.Create(f, raycastShot);
			}
		}
	}
}