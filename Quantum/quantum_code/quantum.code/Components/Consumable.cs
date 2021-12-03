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
					f.Unsafe.GetPointer<Weapon>(playerEntity)->GainAmmo(consumable.Amount);
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

		/// <summary>
		/// Decide what to spawn when a player collects a stash
		/// </summary>
		private void HandleCollectedStash(Frame f, EntityRef e, int stashValue)
		{
			var gameIdsToDrop = new List<GameId>();
			var weaponIDs = GameIdGroup.Weapon.GetIds();
			var stashPosition = f.Get<Transform3D>(e).Position;
			
			switch (stashValue)
			{
				// Legendary stash
				case 3 :
					gameIdsToDrop.Add(weaponIDs[f.RNG->Next(0, weaponIDs.Count)]);
					gameIdsToDrop.Add(GameId.InterimArmourLarge);
					gameIdsToDrop.Add(GameId.Health);
					break;
				
				// Rare stash
				case 2 :
					gameIdsToDrop.Add(weaponIDs[f.RNG->Next(0, weaponIDs.Count)]);
					if (f.RNG->Next() <= FP._0_25)
					{
						gameIdsToDrop.Add(GameId.InterimArmourLarge);
					}
					else
					{
						gameIdsToDrop.Add(GameId.InterimArmourSmall);
					}
					gameIdsToDrop.Add(GameId.Health);
					break;
				
				// Common stash
				case 1 :
				default :
					if (f.RNG->Next() <= FP._0_10)
					{
						var randomWeaponId = weaponIDs[f.RNG->Next(0, weaponIDs.Count)];
						gameIdsToDrop.Add(randomWeaponId);
					}
					if (f.RNG->Next() <= FP._0_25)
					{
						gameIdsToDrop.Add(GameId.InterimArmourSmall);
					}
					for (var i = gameIdsToDrop.Count; i < 2; i++)
					{
						gameIdsToDrop.Add(GameId.Health);
					}
					break;
			}
			
			var angleStep = FP.PiTimes2 / gameIdsToDrop.Count;
			
			for (var i = 0; i < gameIdsToDrop.Count; i++)
			{
				var dropPosition = stashPosition +
				                   (FPVector2.Rotate(FPVector2.Left, angleStep * i) * Constants.DROP_OFFSET_RADIUS).XOY;
				QuantumHelpers.TryFindPosOnNavMesh(f, EntityRef.None, dropPosition,
				                                   out FPVector3 correctedPosition);
				DropCollectable(f, gameIdsToDrop[i], correctedPosition);
			}
		}
		
		private void DropCollectable(Frame f, GameId dropItemGameId, FPVector3 dropPosition)
		{
			switch (dropItemGameId)
			{
				case GameId.Health:
				case GameId.InterimArmourLarge:
				case GameId.InterimArmourSmall:
					var configConsumable = f.ConsumableConfigs.GetConfig(dropItemGameId);
					var entityConsumable = f.Create(f.FindAsset<EntityPrototype>(configConsumable.AssetRef.Id));
					f.Unsafe.GetPointer<Consumable>(entityConsumable)->Init(f, entityConsumable, dropPosition, FPQuaternion.Identity, configConsumable);
					break;
				default:
					var configWeapon = f.WeaponConfigs.GetConfig(dropItemGameId);
					var entityWeapon = f.Create(f.FindAsset<EntityPrototype>(configWeapon.AssetRef.Id));
					f.Unsafe.GetPointer<WeaponCollectable>(entityWeapon)->Init(f, entityWeapon, dropPosition, FPQuaternion.Identity, configWeapon);
					break;
			}
		}
	}
}