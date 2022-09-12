using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public struct QuantumWeaponConfig
	{
		public GameId Id;
		public QuantumGameModePair<FP> InitialAmmoFilled;
		public QuantumGameModePair<int> MaxAmmo;
		public FP AimingMovementSpeed;
		public FP AimTime;
		public FP AttackCooldown;
		public FP PowerToDamageRatio;
		public FP AttackHitSpeed;
		public uint MinAttackAngle;
		public uint MaxAttackAngle;
		public uint NumberOfShots;
		public uint NumberOfBursts;
		public bool CanHitSameTarget;
		public bool IsProjectile;
		public FP SplashRadius;
		public FP SplashDamageRatio;
		public List<GameId> Specials;
		public FP InitialAttackCooldown;
		public FP InitialAttackRampUpTime;
		public uint KnockbackAmount;

		/// <summary>
		/// Requests if this config is from a melee weapon
		/// <remarks>
		/// We check this against the BattleRoyale value, but it's always
		/// the same for both BR and DM.
		/// </remarks>
		/// </summary>
		public bool IsMeleeWeapon => Id == GameId.Hammer;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumWeaponConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumWeaponConfigs
	{
		public List<QuantumWeaponConfig> QuantumConfigs = new List<QuantumWeaponConfig>();
		
		private IDictionary<GameId, QuantumWeaponConfig> _dictionary = null;

		/// <summary>
		/// Requests the <see cref="QuantumWeaponConfig"/> of the given enemy <paramref name="gameId"/>
		/// </summary>
		public QuantumWeaponConfig GetConfig(GameId gameId)
		{
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<GameId, QuantumWeaponConfig>();
				
				foreach (var config in QuantumConfigs)
				{
					_dictionary.Add(config.Id, config);
				}
			}
			

			return _dictionary[gameId];
		}
	}
}