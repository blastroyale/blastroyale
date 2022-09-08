using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial class FrameContextUser
	{
		private readonly List<Equipment> _playersWeaponPool = new List<Equipment>();

		private EquipmentRarity _averageRarity;

		private Dictionary<MutatorType, QuantumMutatorConfig> _mutators;

		public QuantumMapConfig MapConfig { get; internal set; }
		
		public QuantumGameModeConfig GameModeConfig { get; internal set; }

		public List<QuantumMutatorConfig> MutatorConfigs { get; internal set; }
		
		public int TargetAllLayerMask { get; internal set; }

		/// <summary>
		/// Requests the players weapon pool list, ordered by its rarity
		/// </summary>
		public IReadOnlyList<Equipment> GetPlayerWeapons(Frame f, out EquipmentRarity averageRarity)
		{
			if (_playersWeaponPool.Count > 0)
			{
				averageRarity = _averageRarity;

				return _playersWeaponPool;
			}

			var offPool = GameIdGroup.Weapon.GetIds();
			var rarity = 0;

			for (var i = 0; i < f.PlayerCount; i++)
			{
				var playerData = f.GetPlayerData(i);

				if (playerData == null)
				{
					continue;
				}

				var weapon = playerData.Loadout.FirstOrDefault(e => e.IsWeapon());

				if (weapon.IsValid())
				{
					rarity += (int) weapon.Rarity;

					_playersWeaponPool.Add(weapon);
				}
			}

			// Fill up weapon pool to a minimum size
			var poolIndex = _playersWeaponPool.Count;
			while (_playersWeaponPool.Count < Constants.OFFHAND_POOLSIZE)
			{
				var gameId = offPool[poolIndex++];

				if (gameId == GameId.Hammer) continue;

				var equipment = new Equipment(gameId);
				rarity += (int) equipment.Rarity;
				_playersWeaponPool.Add(equipment);
			}

			averageRarity = (EquipmentRarity) FPMath.FloorToInt((FP) rarity / _playersWeaponPool.Count);

			// We only save the list on verified frames to avoid de-syncs
			if (f.IsPredicted)
			{
				var predictedList = new List<Equipment>(_playersWeaponPool);

				_playersWeaponPool.Clear();

				return predictedList;
			}

			_averageRarity = averageRarity;

			return _playersWeaponPool;
		}

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

			if (_mutators.ContainsKey(type))
			{
				quantumMutatorConfig = _mutators[type];
				return true;
			}

			quantumMutatorConfig = default;
			return false;
		}
	}
}