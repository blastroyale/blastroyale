using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// TODO: Refactor this system when we have the spells
	/// </summary>
	public unsafe class PlayerChargingSystem : SystemMainThreadFilter<PlayerChargingSystem.PlayerCharacterFilter>, 
	                                           ISignalOnTriggerEnter3D 
	{
		public struct PlayerCharacterFilter
		{
			public EntityRef Entity;
			public Transform3D* Transform;
			public PlayerCharging* PlayerCharging;
			public AlivePlayerCharacter* AlivePlayer;
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref PlayerCharacterFilter filter)
		{
			var charging = filter.PlayerCharging;
			var lerpT = FPMath.Clamp01((f.Time - charging->ChargeStartTime) / charging->ChargeDuration);
			var nextPos2d = FPVector2.Lerp(charging->ChargeStartPos.XZ, charging->ChargeEndPos.XZ, lerpT);
			var nextPosY = FPMath.Lerp(charging->ChargeStartPos.Y, charging->ChargeEndPos.Y, lerpT);
			var nextPos = new FPVector3(nextPos2d.X, nextPosY, nextPos2d.Y);
				
			filter.Transform->Position = nextPos;
				
			if (f.Time > charging->ChargeStartTime + charging->ChargeDuration)
			{
				f.Unsafe.GetPointer<PhysicsCollider3D>(filter.Entity)->IsTrigger = false;
				f.Remove<PlayerCharging>(filter.Entity);
			}
		}

		/// <inheritdoc />
		public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
		{
			var targetHit = info.Other;
			var attacker = info.Entity;
			
			if (!f.TryGet<PlayerCharging>(attacker, out var charging))
			{
				return;
			}
			
			if(f.TryGet<Stats>(targetHit, out var targetStats))
			{
				var targetMaxHP = targetStats.GetStatData(StatType.Health).BaseValue;
				var damage = targetMaxHP * charging.PowerAmount;

				var spell = Spell.CreateInstant(f, targetHit, attacker, attacker, (uint)damage, 0,
											f.Get<Transform3D>(targetHit).Position);
				QuantumHelpers.ProcessHit(f, spell);
			}

			

			
		}
	}
}