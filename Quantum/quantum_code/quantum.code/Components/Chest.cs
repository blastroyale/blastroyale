using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Chest
	{
		/// <summary>
		/// Initializes this Chest with all the necessary data
		/// </summary>
		internal void Init(Frame f, EntityRef e, FPVector3 position, FPQuaternion rotation,
		                   QuantumChestConfig config)
		{
			var collectable = new Collectable {GameId = config.Id};
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			ChestType = config.ChestType;

			transform->Position = position;
			transform->Rotation = rotation;

			f.Add(e, collectable);
		}

		public void Open(Frame f, EntityRef e, EntityRef playerEntity)
		{
			// TODO mihak: Implement chest opening logic
		}

		private void HandleCollectedStash(Frame f, EntityRef e, int stashValue)
		{
			var weaponIDs = new List<GameId>(GameIdGroup.Weapon.GetIds());
			var stashPosition = f.Get<Transform3D>(e).Position;

			// Choose only non-melee weapons to consider for a drop
			for (var i = weaponIDs.Count - 1; i > -1; i--)
			{
				if (f.WeaponConfigs.GetConfig(weaponIDs[i]).IsMeleeWeapon)
				{
					weaponIDs.RemoveAt(i);
				}
			}

			switch (stashValue)
			{
				// Legendary stash
				case 3:
					Collectable.DropCollectable(f, weaponIDs[f.RNG->Next(0, weaponIDs.Count)], stashPosition, 0, true);
					Collectable.DropCollectable(f, weaponIDs[f.RNG->Next(0, weaponIDs.Count)], stashPosition, 1, true);

					var armourType = f.RNG->Next() < FP._0_75 ? GameId.ShieldLarge : GameId.ShieldSmall;
					Collectable.DropCollectable(f, armourType, stashPosition, 2, false);

					var ammoType = f.RNG->Next() < FP._0_50 ? GameId.AmmoLarge : GameId.AmmoSmall;
					Collectable.DropCollectable(f, ammoType, stashPosition, 3, false);
					Collectable.DropCollectable(f, GameId.AmmoSmall, stashPosition, 3, false);

					Collectable.DropCollectable(f, GameId.Health, stashPosition, 4, false);
					Collectable.DropCollectable(f, GameId.Health, stashPosition, 4, false);

					break;

				// Rare stash
				case 2:
					var armour = f.RNG->Next() < FP._0_25 ? GameId.ShieldLarge : GameId.ShieldSmall;
					var ammoOrHealthChance = f.RNG->Next();

					Collectable.DropCollectable(f, weaponIDs[f.RNG->Next(0, weaponIDs.Count)], stashPosition, 0, true);
					Collectable.DropCollectable(f, armour, stashPosition, 1, false);

					Collectable.DropCollectable(f, GameId.AmmoSmall, stashPosition, 1, false);
					Collectable.DropCollectable(f, GameId.AmmoSmall, stashPosition, 1, false);

					if (ammoOrHealthChance < FP._0_20 + FP._0_10)
					{
						Collectable.DropCollectable(f, GameId.AmmoLarge, stashPosition, 2, false);
					}
					else if (ammoOrHealthChance < FP._0_50 + FP._0_20 + FP._0_20)
					{
						Collectable.DropCollectable(f, GameId.AmmoSmall, stashPosition, 2, false);
					}
					else
					{
						Collectable.DropCollectable(f, GameId.Health, stashPosition, 2, false);
					}

					break;

				// Common stash
				default:
					if (f.RNG->Next() <= FP._0_05)
					{
						Collectable.DropCollectable(f, weaponIDs[f.RNG->Next(0, weaponIDs.Count)], stashPosition, 0,
						                            true);
					}
					else if (f.RNG->Next() <= FP._0_50 + FP._0_20)
					{
						Collectable.DropCollectable(f, GameId.AmmoSmall, stashPosition, 0, false);
					}
					else
					{
						Collectable.DropCollectable(f, GameId.Health, stashPosition, 0, false);
					}

					if (f.RNG->Next() <= FP._0_10)
					{
						Collectable.DropCollectable(f, GameId.AmmoLarge, stashPosition, 1, false);
					}
					else if (f.RNG->Next() <= FP._0_50)
					{
						Collectable.DropCollectable(f, GameId.ShieldSmall, stashPosition, 1, false);
					}
					else
					{
						Collectable.DropCollectable(f, GameId.Health, stashPosition, 1, false);
					}

					if (f.RNG->Next() <= FP._0_50 + FP._0_20 + FP._0_20)
					{
						Collectable.DropCollectable(f, GameId.AmmoSmall, stashPosition, 2, false);
					}
					else
					{
						Collectable.DropCollectable(f, GameId.Health, stashPosition, 2, false);
					}

					break;
			}
		}
	}
}