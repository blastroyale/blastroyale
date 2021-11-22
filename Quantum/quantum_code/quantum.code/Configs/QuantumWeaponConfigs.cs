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
		public ItemRarity StartingRarity;
		public IndicatorVfxId Indicator;
		public FP PowerRatioToBase;
		public bool IsAutoShoot;
		public FP AimingMovementSpeedMultiplier;
		public FP AimTime;
		public FP AttackCooldown;
		public uint InitialCapacity;
		public uint MaxCapacity;
		public uint MinCapacityToShoot;
		public ReloadType ReloadType;
		public FP ReloadSpeed;
		public TargetingType TargetingType;
		public FP SplashRadius;
		public FP TargetRange;
		public GameId ProjectileId;
		public GameId ProjectileHealingId;
		public uint BulletSpreadAngle;
		public FP ProjectileSpeed;
		public FP ProjectileRange;
		public FP ProjectileStunDuration;
		public GameId HazardId;
		public List<GameId> Specials;
		public bool IsDiagonalshot;
		public bool IsMultishot;
		public bool IsFrontshot;
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