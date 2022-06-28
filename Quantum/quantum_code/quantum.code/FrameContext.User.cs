using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;

namespace Quantum 
{
	public unsafe partial class FrameContextUser
	{
		private readonly List<Equipment> _playersWeaponPool = new List<Equipment>();

		private EquipmentRarity _averageRarity;
		
		public QuantumMapConfig MapConfig { get; internal set; }
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
					rarity += (int)weapon.Rarity;
						
					_playersWeaponPool.Add(weapon);
				}
			}

			// Fill up weapon pool to a minimum size
			for (var i = _playersWeaponPool.Count; i < Constants.OFFHAND_POOLSIZE; i++)
			{
				var equipment = new Equipment(offPool[i]);
					
				rarity += (int)equipment.Rarity;
					
				_playersWeaponPool.Add(equipment);
			}

			averageRarity = (EquipmentRarity)FPMath.FloorToInt((FP) rarity / _playersWeaponPool.Count);

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
	}
}