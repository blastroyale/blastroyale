using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles the drop of consumables when player dies
	/// TODO: Move all chances into a proper Google Sheet config
	/// TODO: Add equipment drop chances when it will be possible to drop equipment
	/// </summary>
	public unsafe class DropOnDeathSystem : SystemSignalsOnly, ISignalPlayerKilledPlayer
	{
		/// <inheritdoc />
		public void PlayerKilledPlayer(Frame f, PlayerRef playerDead, EntityRef entityDead, PlayerRef playerKiller,
		                               EntityRef entityKiller)
		{
			// Drop chances data and other parameters
			var healthChance = FP._1;
			var largeArmourChance = FP._0_01;
			var smallArmourChance = FP._0_25 + largeArmourChance;
			var weaponChance = FP._0_50;
			var offsetRadius = FP._1_75;
			
			var gameIdsToDrop = new List<GameId>();
			var deathPosition = f.Get<Transform3D>(entityDead).Position;
			
			// Try to drop Health pack
			if (f.RNG->Next() <= healthChance)
			{
				gameIdsToDrop.Add(GameId.Health);
			}
			
			// Try to drop InterimArmourLarge, if didn't work then try to drop InterimArmourSmall
			var armourDropChance = f.RNG->Next();
			if (armourDropChance <= largeArmourChance)
			{
				gameIdsToDrop.Add(GameId.InterimArmourLarge);
			}
			else if (armourDropChance <= smallArmourChance)
			{
				gameIdsToDrop.Add(GameId.InterimArmourSmall);
			}
			
			// Try to drop Weapon
			if (f.RNG->Next() <= weaponChance && f.TryGet<Weapon>(entityDead, out var weapon))
			{
				gameIdsToDrop.Add(weapon.GameId);
			}
			
			// Return if there's nothing to drop
			if (gameIdsToDrop.Count == 0)
			{
				return;
			}
			
			var angleStep = FP.PiTimes2 / gameIdsToDrop.Count;
			
			for (var i = 0; i < gameIdsToDrop.Count; i++)
			{
				var dropPosition = deathPosition +
				                   (FPVector2.Rotate(FPVector2.Left, angleStep * i) * offsetRadius).XOY;
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