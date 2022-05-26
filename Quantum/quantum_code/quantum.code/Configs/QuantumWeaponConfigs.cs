using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumWeaponConfig
	{
		public GameId Id;
		public AssetRefEntityPrototype AssetRef;
		public QuantumGameModePair<FP> InitialAmmoFilled;
		public QuantumGameModePair<int> MaxAmmo;
		public FP AimingMovementSpeed;
		public FP AimTime;
		public FP AttackCooldown;
		public FP AttackHitSpeed;
		public uint MinAttackAngle;
		public uint MaxAttackAngle;
		public uint NumberOfShots;
		public FP AttackRange;
		public bool CanHitSameTarget;
		public bool IsProjectile;
		public FP SplashRadius;
		public FP SplashDamageRatio;
		public List<GameId> Specials;
		

		/// <summary>
		/// Requests if this config is from a melee weapon
		/// </summary>
		public bool IsMeleeWeapon => MaxAmmo.BattleRoyale < 0;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumWeaponConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumWeaponConfigs
	{
		public List<QuantumWeaponConfig> QuantumConfigs = new List<QuantumWeaponConfig>();
		
		private IDictionary<GameId, QuantumWeaponConfig> _dictionary = new Dictionary<GameId, QuantumWeaponConfig>();

		/// <summary>
		/// Requests the <see cref="QuantumGearConfig"/> of the given enemy <paramref name="gameId"/>
		/// </summary>
		public QuantumWeaponConfig GetConfig(GameId gameId)
		{
			if (_dictionary.Count == 0)
			{
				foreach (var config in QuantumConfigs)
				{
					_dictionary.Add(config.Id, config);
				}
			}
			

			return _dictionary[gameId];
		}
	}
}