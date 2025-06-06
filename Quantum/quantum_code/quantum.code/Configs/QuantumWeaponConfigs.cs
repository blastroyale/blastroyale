using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	public enum SubProjectileHitType
	{
		/// <summary>
		/// Lasts for one second
		/// Can only hit once
		/// Will spawn a hitbox then de-spawn the hitbox after one second
		/// Ideal for explosions and quick AOE attacks
		/// </summary>
		AreaOfEffect
	}

	[Serializable]
	public class QuantumWeaponConfig
	{
		public GameId Id;
		public FiringMode FiringMode;
		public WeaponType WeaponType;
		public int MaxAmmo;
		public AssetRefEntityPrototype BulletPrototype;
		public AssetRefEntityPrototype BulletHitPrototype;
		public AssetRefEntityPrototype BulletEndOfLifetimePrototype;
		public SubProjectileHitType HitType;
		public int MagazineSize;
		public FP ReloadTime;
		public FP AimingMovementSpeed;
		public FP TapCooldown;
		public FP AttackCooldown;
		public FP PowerToDamageRatio;
		public FP AttackHitSpeed;
		public uint MinAttackAngle;
		public uint NumberOfShots;
		public uint NumberOfBursts;
		public FP AttackRange;
		public bool CanHitSameTarget;
		public FP SplashRadius;
		public FP SplashDamageRatio;
		public FP InitialAttackCooldown;
		public FP InitialAttackRampUpTime;
		public bool UseRangedCam;
		public FP AimDelayTime;
		public FP BurstGapTime;

		/// <summary>
		/// Requests if this config is from a melee weapon
		/// <remarks>
		/// We check this against the BattleRoyale value, but it's always
		/// the same for both BR and DM.
		/// </remarks>
		/// </summary>
		public bool IsMeleeWeapon => Id == GameId.Hammer;
	}

	public enum FiringMode
	{
		FullyAutomatic = 0,
		SemiAutomatic = 1,
	}

	public enum WeaponType
	{
		// 0 is "no weapon" in animations so we keep the same numbering here.
		Melee = 1,
		Gun = 2,
		XLGun = 3,
		SniperGun = 4,
		Launcher = 5,
		XLMelee = 6,
		KnifeMelee = 7,
		Handgun = 8,
		Shotgun = 9
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumWeaponConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumWeaponConfigs
	{
		public FP GoldenGunDamageModifier = FP._1_20;

		public List<QuantumWeaponConfig> QuantumConfigs = new List<QuantumWeaponConfig>();

		private IDictionary<GameId, QuantumWeaponConfig> _dictionary = null;

		private IDictionary<GameId, List<int>> BakedAccuracyMods = null;

		/// <summary>
		/// Randomize a baked accuracy angle. Simply picks a random element from
		/// the baked accuracy list to minimize calculations.
		/// </summary>
		public unsafe FP GetRandomBakedAccuracyAngle(Frame f, GameId weaponId)
		{
			var mods = BakedAccuracyMods[weaponId];
			var mod = mods[f.RNG->Next(0, mods.Count)];
			return f.RNG->Next(0, 2) == 1 ? -mod : mod;
		}

		/// <summary>
		/// Pre bakes all accuracy calculations so we dont need to calculate
		/// everytime a shot is fired
		/// </summary>
		private void BakeAngles()
		{
			var bakes = new Dictionary<GameId, List<int>>();
			foreach (var config in QuantumConfigs)
			{
				var accuracies = new List<int>();
				var maxAttackAngle = config.MinAttackAngle;
				if (maxAttackAngle == 0)
				{
					accuracies.Add(0);
				}
				else
				{
					foreach (var distribution in Constants.APPRX_NORMAL_DISTRIBUTION)
					{
						var mod = (int) Math.Round(maxAttackAngle / 100d * distribution);
						accuracies.Add(mod / 2);
					}
				}

				bakes[config.Id] = accuracies;
			}
			BakedAccuracyMods = bakes;
		}
	
		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			BakeAngles();
			var dictionary = new Dictionary<GameId, QuantumWeaponConfig>();
			foreach (var config in QuantumConfigs)
			{
				dictionary.Add(config.Id, config);
			}
			_dictionary = dictionary;
		}

		/// <summary>
		/// Requests the <see cref="QuantumWeaponConfig"/> of the given enemy <paramref name="gameId"/>
		/// </summary>
		public QuantumWeaponConfig GetConfig(GameId gameId)
		{
			TryGetConfig(gameId, out var returnValue);
			return returnValue;
		}

		/// <summary>
		/// Requests the <see cref="QuantumWeaponConfig"/> of the given enemy <paramref name="gameId"/>
		/// </summary>
		public bool TryGetConfig(GameId gameId, out QuantumWeaponConfig configValue)
		{
			return _dictionary.TryGetValue(gameId, out configValue);
		}
	}
}
