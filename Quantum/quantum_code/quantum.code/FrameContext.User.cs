using System.Collections.Generic;

namespace Quantum
{
	public unsafe partial class FrameContextUser
	{
		private Dictionary<MutatorType, QuantumMutatorConfig> _mutators;

		public QuantumMapConfig MapConfig { get; internal set; }
		
		public QuantumGameModeConfig GameModeConfig { get; internal set; }

		public List<QuantumMutatorConfig> MutatorConfigs { get; internal set; }
		
		public IDictionary<int, QuantumShrinkingCircleConfig> MapShrinkingCircleConfigs { get; internal set; }
		
		public int TargetAllLayerMask { get; internal set; }
		
		public int TargetMapOnlyLayerMask { get; internal set; }
		
		public int TargetPlayersMask { get; internal set; }

		/// <summary>
		/// Requests the current game's mutator by type
		/// </summary>
		public bool TryGetMutatorByType(MutatorType type, out QuantumMutatorConfig quantumMutatorConfig)
		{
			if (_mutators == null)
			{
				_mutators = new Dictionary<MutatorType, QuantumMutatorConfig>();
				foreach (var config in MutatorConfigs)
				{
					_mutators[config.Type] = config;
				}
			}

			return _mutators.TryGetValue(type, out quantumMutatorConfig);
		}
		
		// TODO: Refactor this hardcode
		/// <summary>
		/// Requests the weapon GameId if forced by mutator
		/// </summary>
		public bool TryGetWeaponLimiterMutator(out GameId weaponLimitId)
		{
			if (TryGetMutatorByType(MutatorType.PistolsOnly, out _))
			{
				weaponLimitId = GameId.ModPistol;
				return true;
			}
			if (TryGetMutatorByType(MutatorType.SMGsOnly, out _))
			{
				weaponLimitId = GameId.ApoSMG;
				return true;
			}
			if (TryGetMutatorByType(MutatorType.MinigunsOnly, out _))
			{
				weaponLimitId = GameId.ModHeavyMachineGun;
				return true;
			}
			if (TryGetMutatorByType(MutatorType.ShotgunsOnly, out _))
			{
				weaponLimitId = GameId.ApoShotgun;
				return true;
			}
			if (TryGetMutatorByType(MutatorType.SnipersOnly, out _))
			{
				weaponLimitId = GameId.ModSniper;
				return true;
			}
			if (TryGetMutatorByType(MutatorType.RPGsOnly, out _))
			{
				weaponLimitId = GameId.SciCannon;
				return true;
			}
			
			weaponLimitId = default(GameId);
			return false;
		}
	}
}