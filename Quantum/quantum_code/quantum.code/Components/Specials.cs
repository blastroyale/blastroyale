using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Has all necessary data and methods to work with Special
	/// </summary>
	public partial struct Special
	{
		public bool IsInfiniteUse => MaxCharges == 0;
		public bool IsAimable => MaxRange > FP._0;
		public bool IsValid => SpecialGameId != default(GameId);
		
		/// <summary>
		/// Initializes this Special with all the necessary data
		/// </summary>
		public Special(Frame f, QuantumSpecialConfig config, int specialIndex) : this()
		{
			SpecialGameId = config.Id;
			SpecialIndex = specialIndex;
			SpecialType = config.SpecialType;
			BaseCharges = config.BaseCharges;
			MaxCharges = config.MaxCharges;
			Cooldown = config.Cooldown;
			SplashRadius = config.SplashRadius;
			PowerAmount = config.PowerAmount;
			ResetCooldownTime = f.Time + config.InitialCooldown;
			Charges = config.BaseCharges;
			Speed = config.Speed;
			ExtraGameId = config.ExtraId;
			MaxRange = config.MaxRange;
			HealingModeSwitchTime = FP.MaxValue;
		}
		
		/// <summary>
		/// Checks if special is available to be used
		/// </summary>
		public bool IsSpecialAvailable(Frame f)
		{
			return f.Time >= ResetCooldownTime && (IsInfiniteUse || Charges > 0);
		}
		
		/// <summary>
		/// Reduces the charge of this special, set it on cooldown and sends an event
		/// </summary>
		public void HandleUsed(Frame f, EntityRef playerEntity, PlayerRef playerRef)
		{
			HandleUsed(f, playerEntity);

			f.Signals.SpecialUsed(playerRef, playerEntity, SpecialType, SpecialIndex);
			f.Events.OnLocalSpecialUsed(playerRef, playerEntity, SpecialType, SpecialIndex);
		}
		
		/// <summary>
		/// Reduces the charge of this special and set it on cooldown
		/// </summary>
		public void HandleUsed(Frame f, EntityRef playerEntity)
		{
			ResetCooldownTime = f.Time + Cooldown;
			if (!IsInfiniteUse)
			{
				Charges -= 1;
			}
		}
		
		/// <summary>
		/// Resets the cooldown of this special
		/// </summary>
		public void ResetCooldown(Frame f, EntityRef playerEntity, PlayerRef playerRef)
		{
			ResetCooldownTime = f.Time;
			
			f.Events.OnLocalSpecialAvailable(playerRef, playerEntity, SpecialType, SpecialIndex);
		}
		
		/// <summary>
		/// Increase the charge of this special
		/// </summary>
		public void IncreaseCharge(Frame f, EntityRef playerEntity, PlayerRef playerRef)
		{
			Charges = Charges < MaxCharges ? Charges + 1 : MaxCharges;
			
			f.Events.OnLocalSpecialAvailable(playerRef, playerEntity, SpecialType, SpecialIndex);
		}
		
		/// <summary>
		/// Tries to use a special depending on its type and return success or failure result
		/// </summary>
		public bool TryUse(Frame f, EntityRef entity, FPVector2 aimInput)
		{
			switch (SpecialType)
			{
				case SpecialType.Airstrike:
					return SpecialAirstrike.Use(f, entity, this, aimInput, MaxRange);
				case SpecialType.HealingField:
					return SpecialHealingField.Use(f, entity, this);
				case SpecialType.StunSplash:
				case SpecialType.HealAround:
					return SpecialAreaDamage.Use(f, entity, this);
				case SpecialType.RageSelfStatus:
				case SpecialType.InvisibilitySelfStatus:
				case SpecialType.ShieldSelfStatus:
					return SpecialSelfStatusModifier.Use(f, entity, this);
				case SpecialType.StunGrenade:
					return SpecialStunGrenade.Use(f, entity, this, aimInput, MaxRange);
				case SpecialType.RageAimAreaStatus:
				case SpecialType.InvisibilityAimAreaStatus:
				case SpecialType.ShieldAimAreaStatus:
					return SpecialAimAreaStatusModifier.Use(f, entity, this, aimInput, MaxRange);
				case SpecialType.HealAim:
					return SpecialAimAreaDamage.Use(f, entity, this, aimInput, MaxRange);
				case SpecialType.HealingMode:
					return SpecialHealingMode.Use(f, entity, this);
				case SpecialType.HazardAimSpawn:
					return SpecialHazardAimSpawn.Use(f, entity, this, aimInput, MaxRange);
				case SpecialType.AggroBeaconGrenade:
					return SpecialAggroBeaconGrenade.Use(f, entity, this, aimInput, MaxRange);
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