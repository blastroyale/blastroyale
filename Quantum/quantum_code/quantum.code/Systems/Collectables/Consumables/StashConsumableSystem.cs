using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Systems.Collectables.Consumables
{
	/// <inheritdoc />
	/// <remarks>
	/// Implementation for the <see cref="Stash"/> <see cref="Consumable"/> in the game.
	/// </remarks>
	public unsafe class StashConsumableSystem : ConsumableSystemBase
	{
		protected override ConsumableType ConsumableType => ConsumableType.Stash;

		protected override void OnConsumablePicked(Frame f, EntityRef e, EntityRef playerEntity, PlayerRef player, 
		                                           Consumable consumable)
		{
			var gameIdsToDrop = new List<GameId>();
			var weaponIDs = GameIdGroup.Weapon.GetIds();
			var stashPosition = f.Get<Transform3D>(e).Position;
			
			switch ((int) consumable.PowerAmount)
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