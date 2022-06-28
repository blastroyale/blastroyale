using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Has all necessary data and methods to work with Special
	/// </summary>
	public partial struct Special
	{
		public bool IsAimable => MaxRange > FP._0;
		public bool IsValid => SpecialId != GameId.Random;
		
		/// <summary>
		/// Initializes this Special with all the necessary data
		/// </summary>
		/// 
		
		public Special(Frame f, QuantumSpecialConfig config) : this()
		{
			SpecialId = config.Id;
			SpecialType = config.SpecialType;
			Cooldown = config.Cooldown;
			Radius = config.Radius;
			SpecialPower = config.SpecialPower;
			Speed = config.Speed;
			MaxRange = config.MaxRange;
			AvailableTime = f.Time + config.Cooldown;
			Knockback = config.Knockback;
			PercentHealthDamage = config.PercentHealthDamage;
		}
		
		/// <summary>
		/// Checks if special is available to be used
		/// </summary>
		public bool IsSpecialAvailable(Frame f)
		{
			return AvailableTime > FP._0 && f.Time >= AvailableTime;
		}

		/// <summary>
		/// Tries to activate this special and returns true if was successfully activated, returns false otherwise
		/// </summary>
		public bool TryActivate(Frame f, EntityRef playerEntity, FPVector2 aimInput, int specialIndex)
		{
			if (!IsValid || !IsSpecialAvailable(f) || !TryUse(f, playerEntity, aimInput))
			{
				return false;
			}
			
			var player = f.Get<PlayerCharacter>(playerEntity).Player;

			AvailableTime = FP._0;
			
			f.Signals.SpecialUsed(player, playerEntity, SpecialType, specialIndex);
			f.Events.OnSpecialUsed(player, playerEntity, SpecialType, specialIndex);
			f.Events.OnLocalSpecialUsed(player, playerEntity, SpecialType, specialIndex);

			return true;
		}
		
		private bool TryUse(Frame f, EntityRef entity, FPVector2 aimInput)
		{
			switch (SpecialType)
			{
				case SpecialType.Airstrike:
					return SpecialAirstrike.Use(f, entity, this, aimInput, MaxRange);
				case SpecialType.ShieldSelfStatus:
					return SpecialSelfStatusModifier.Use(f, entity, this);
				case SpecialType.StunGrenade:
					return SpecialStunGrenade.Use(f, entity, this, aimInput, MaxRange);
				case SpecialType.HazardAimSpawn:
					return SpecialHazardAimSpawn.Use(f, entity, this, aimInput, MaxRange);
				case SpecialType.ShieldedCharge:
					return SpecialShieldedCharge.Use(f, entity, this, aimInput, MaxRange);
				case SpecialType.Grenade:
					return SpecialGrenade.Use(f, entity, this, aimInput, MaxRange);
				default:
					return false;
			}
		}
	}
}