using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// TODO: Refactor this system when we have the spells
	/// </summary>
	public unsafe class PlayerChargingSystem : SystemMainThreadFilter<PlayerChargingSystem.PlayerCharacterFilter>, 
	                                           ISignalOnTriggerEnter2D 
	{
		public struct PlayerCharacterFilter
		{
			public EntityRef Entity;
			public Transform2D* Transform;
			public PlayerCharging* PlayerCharging;
			public AlivePlayerCharacter* AlivePlayer;
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref PlayerCharacterFilter filter)
		{
			var charging = filter.PlayerCharging;
			var lerpT = FPMath.Clamp01((f.Time - charging->ChargeStartTime) / charging->ChargeDuration);
			var nextPos2d = FPVector2.Lerp(charging->ChargeStartPos, charging->ChargeEndPos, lerpT);

			filter.Transform->Position = nextPos2d;
				
			if (f.Time > charging->ChargeStartTime + charging->ChargeDuration)
			{
				f.Remove<PlayerCharging>(filter.Entity);
			}
		}

		/// <inheritdoc />
		public void OnTriggerEnter2D(Frame f, TriggerInfo2D info)
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
											f.Unsafe.GetPointer<Transform2D>(targetHit)->Position);
				QuantumHelpers.ProcessHit(f, &spell);
			}

			

			
		}
	}
}