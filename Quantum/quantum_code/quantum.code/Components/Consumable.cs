using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Consumable
	{
		/// <summary>
		/// Initializes this Consumable with all the necessary data
		/// </summary>
		internal void Init(Frame f, EntityRef e, FPVector3 position, FPQuaternion rotation, QuantumConsumableConfig config)
		{
			var collectable = new Collectable {GameId = config.Id};
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			
			ConsumableType = config.ConsumableType;
			Amount = config.PowerAmount;
			CollectTime = config.ConsumableCollectTime;
			
			transform->Position = position;
			transform->Rotation = rotation;
			
			f.Add(e, collectable);
		}

		/// <summary>
		/// Collects this given <paramref name="entity"/> by the given <paramref name="player"/>
		/// </summary>
		internal void Collect(Frame f, EntityRef entity, EntityRef playerEntity, PlayerRef player)
		{
			var consumable = f.Get<Consumable>(entity);

			switch (ConsumableType)
			{
				case ConsumableType.Health:
					f.Unsafe.GetPointer<Stats>(playerEntity)->GainHealth(f, playerEntity, entity, (int) consumable.Amount);
					break;
				case ConsumableType.Rage:
					StatusModifiers.AddStatusModifierToEntity(f, playerEntity, StatusModifierType.Rage, (int) consumable.Amount);
					break;
				case ConsumableType.Ammo:
					f.Unsafe.GetPointer<PlayerCharacter>(playerEntity)->GainAmmo(f, playerEntity, consumable.Amount);
					break;
				case ConsumableType.InterimArmour:
					f.Unsafe.GetPointer<Stats>(playerEntity)->GainInterimArmour(f, playerEntity, entity, (int) consumable.Amount);
					break;
				case ConsumableType.Stash:
					HandleCollectedStash(f, entity, (int) consumable.Amount);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void HandleCollectedStash(Frame f, EntityRef e, int stashValue)
		{
			var unfilteredWeaponIDs = GameIdGroup.Weapon.GetIds();
			var stashPosition = f.Get<Transform3D>(e).Position;
			var weaponIDs = new List<GameId>();
			
			// Choose only non-melee weapons to consider for a drop
			for (int i = 0; i < unfilteredWeaponIDs.Count; i++)
			{
				if (f.WeaponConfigs.GetConfig(unfilteredWeaponIDs[i]).InitialAmmo < 0)
				{
					continue;
				}
				
				weaponIDs.Add(unfilteredWeaponIDs[i]);
			}
			
			switch (stashValue)
			{
				// Legendary stash
				case 3 :
					Collectable.DropCollectable(f, weaponIDs[f.RNG->Next(0, weaponIDs.Count)], stashPosition, 0, true);
					Collectable.DropCollectable(f, GameId.InterimArmourLarge, stashPosition, 1, false);
					Collectable.DropCollectable(f, GameId.AmmoLarge, stashPosition, 2, false);
					break;
				
				// Rare stash
				case 2 :
					var armour = f.RNG->Next() < FP._0_25 ? GameId.InterimArmourLarge : GameId.InterimArmourSmall;
					var ammoOrHealthChance = f.RNG->Next();
					
					Collectable.DropCollectable(f, weaponIDs[f.RNG->Next(0, weaponIDs.Count)], stashPosition, 0, true);
					Collectable.DropCollectable(f, armour, stashPosition, 1, false);
					
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
				default :
					if (f.RNG->Next() <= FP._0_05)
					{
						Collectable.DropCollectable(f, weaponIDs[f.RNG->Next(0, weaponIDs.Count)], stashPosition, 0, true);
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
						Collectable.DropCollectable(f, GameId.InterimArmourSmall, stashPosition, 1, false);
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