using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;

namespace Quantum 
{
	public unsafe partial class FrameContextUser
	{
		private readonly List<Equipment> _playersWeaponPool = new List<Equipment>();
		
		public QuantumMapConfig MapConfig { get; internal set; }
		public int TargetAllLayerMask { get; internal set; }

		/// <summary>
		/// Obtains the current saved median rarity from the weapon pool
		/// DON'T MAKE THIS METHOD PUBLIC
		/// </summary>
		private EquipmentRarity MedianRarity =>
			_playersWeaponPool[FPMath.FloorToInt(_playersWeaponPool.Count / FP._2)].Rarity;

		/// <summary>
		/// Requests the players weapon pool list, ordered by its rarity
		/// </summary>
		public IReadOnlyList<Equipment> GetPlayerWeapons(Frame f, out EquipmentRarity medianRarity)
		{
			if (f.PlayerCount != _playersWeaponPool.Count)
			{
				var offPool = Constants.OFFHAND_WEAPON_POOL;
				
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
						_playersWeaponPool.Add(weapon);
					}
				}

				// Fill up weapon pool to a minimum size
				for (var i = _playersWeaponPool.Count; i < offPool.Length; i++)
				{
					_playersWeaponPool.Add(new Equipment(offPool[i]));
				}
			}
			
			_playersWeaponPool.Sort((a, b) => ((int)a.Rarity).CompareTo((int) b.Rarity));

			medianRarity = MedianRarity;
				
			return _playersWeaponPool;
		}
	}
}