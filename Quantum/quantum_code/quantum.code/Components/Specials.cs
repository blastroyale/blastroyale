using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Has all necessary data and methods to work with Special
	/// </summary>
	public unsafe partial struct Special
	{
		public bool IsAimable => MaxRange > FP._0;
		public bool IsValid => SpecialId != GameId.Random;
		
		/// <summary>
		/// Initializes this Special with all the necessary data
		/// </summary>
		public Special(Frame f, GameId specialId) : this()
		{
			var config = f.SpecialConfigs.GetConfig(specialId);
			var specialsCooldownsMutatorExists = f.Context.TryGetMutatorByType(MutatorType.SpecialsCooldowns, out var specialsCooldownsMutatorConfig);

			SpecialId = specialId;
			SpecialType = config.SpecialType;
			Cooldown = specialsCooldownsMutatorExists?specialsCooldownsMutatorConfig.Param1:config.Cooldown;
			Radius = config.Radius;
			SpecialPower = config.SpecialPower;
			Speed = config.Speed;
			MinRange = config.MinRange;
			MaxRange = config.MaxRange;
			InitialCooldown = config.InitialCooldown;
			Knockback = config.Knockback;
			AvailableTime = f.Time + InitialCooldown;
			Charges = 1;
		}

		/// <summary>
		/// Requests the state of the special if is ready to be used or not
		/// </summary>
		public bool IsUsable(Frame f)
		{
			return IsValid && Charges > 0 && f.Time > AvailableTime;
		}

		/// <summary>
		/// Tries to activate this special and returns true if was successfully activated, returns false otherwise
		/// </summary>
		public bool TryActivate(Frame f, PlayerRef playerRef, EntityRef playerEntity, FPVector2 aimInput,
								int specialIndex)
		{
			if (!IsUsable(f) || !TryUse(f, playerEntity, playerRef, aimInput))
			{
				return false;
			}

			AvailableTime = f.Time + Cooldown;
			// TODO Charges--;
			
			f.Signals.SpecialUsed(playerEntity, specialIndex);
			f.Events.OnPlayerSpecialUsed(playerEntity, this, specialIndex, aimInput, MaxRange);

			if (Charges == 0)
			{
				// TODO Trigger update signal
				//f.Unsafe.GetPointer<PlayerInventory>(playerEntity)->Specials[specialIndex] = default;
			}
			
			return true;
		}
		
		private bool TryUse(Frame f, EntityRef entity, PlayerRef playerRef, FPVector2 aimInput)
		{
			switch (SpecialType)
				{
					case SpecialType.Airstrike:
						return SpecialAirstrike.Use(f, entity, ref this, aimInput, MaxRange);
					case SpecialType.ShieldSelfStatus:
						return SpecialSelfStatusModifier.Use(f, entity, ref this );
					case SpecialType.StunGrenade:
						return SpecialStunGrenade.Use(f, entity, ref this , aimInput, MaxRange);
					case SpecialType.HazardAimSpawn:
						return SpecialHazardAimSpawn.Use(f, entity, ref this, aimInput, MaxRange);
					case SpecialType.ShieldedCharge:
						return SpecialShieldedCharge.Use(f, entity, ref this, aimInput, MaxRange);
					case SpecialType.Grenade:
						return SpecialGrenade.Use(f, entity, ref this, aimInput, MaxRange);
					case SpecialType.Radar:
						return SpecialRadar.Use(f, entity, playerRef, ref this);
					default:
						return false;
				}
		}
	}
}