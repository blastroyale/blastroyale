using System;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the <see cref="Collectable"/> component collection interactions using triggers 
	/// </summary>
	public unsafe class CollectableSystem : SystemSignalsOnly, ISignalPlayerDead,
	                                        ISignalOnTriggerEnter3D, ISignalOnTrigger3D, ISignalOnTriggerExit3D
	{
		public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
		{
			if (!f.Unsafe.TryGetPointer<Collectable>(info.Entity, out var collectable) ||
			    !f.Has<AlivePlayerCharacter>(info.Other) || !f.TryGet<PlayerCharacter>(info.Other, out var player) ||
			    f.Has<EntityDestroyer>(info.Entity))
			{
				return;
			}

			if (IsCollectableFilled(f, info.Entity, info.Other))
			{
				f.Events.OnCollectableBlocked(collectable->GameId, info.Entity, player.Player, info.Other);
				return;
			}

			StartCollecting(f, player.Player, info.Other, collectable, info.Entity);
		}

		public void OnTrigger3D(Frame f, TriggerInfo3D info)
		{
			if (!f.Unsafe.TryGetPointer<Collectable>(info.Entity, out var collectable) ||
			    !f.Has<AlivePlayerCharacter>(info.Other) || !f.TryGet<PlayerCharacter>(info.Other, out var player) ||
			    f.Has<EntityDestroyer>(info.Entity))
			{
				return;
			}

			var endTime = collectable->CollectorsEndTime[player.Player];
			if (endTime == FP._0 || f.Time < endTime)
			{
				return;
			}

			if (IsCollectableFilled(f, info.Entity, info.Other))
			{
				f.Events.OnCollectableBlocked(collectable->GameId, info.Entity, player.Player, info.Other);
				StopCollecting(f, info.Entity, info.Other, player.Player, collectable);
				return;
			}

			Collect(f, info.Entity, info.Other, player.Player, collectable);
			
			f.Destroy(info.Entity);
		}

		public void OnTriggerExit3D(Frame f, ExitInfo3D info)
		{
			if (!f.Unsafe.TryGetPointer<Collectable>(info.Entity, out var collectable) ||
			    !f.Has<AlivePlayerCharacter>(info.Other) || !f.TryGet<PlayerCharacter>(info.Other, out var player) ||
			    f.Has<EntityDestroyer>(info.Entity))
			{
				return;
			}

			StopCollecting(f, info.Entity, info.Other, player.Player, collectable);
		}

		private bool IsCollectableFilled(Frame f, EntityRef entity, EntityRef player)
		{
			if (f.Unsafe.TryGetPointer<Consumable>(entity, out var consumable))
			{
				var playerCharacter = f.Get<PlayerCharacter>(player);
				var stats = f.Get<Stats>(player);

				switch (consumable->ConsumableType)
				{
					case ConsumableType.Health:
						return stats.CurrentHealth == stats.GetStatData(StatType.Health).StatValue;
					case ConsumableType.Shield:
						return stats.CurrentShield == stats.GetStatData(StatType.Shield).StatValue;
					case ConsumableType.ShieldCapacity:
						return stats.GetStatData(StatType.Shield).BaseValue ==
						       stats.GetStatData(StatType.Shield).StatValue &&
						       stats.CurrentShield == stats.GetStatData(StatType.Shield).StatValue;
					case ConsumableType.Ammo:
						return playerCharacter.GetAmmoAmountFilled(f, player) == 1;
				}
			}

			return false;
		}

		public void PlayerDead(Frame f, PlayerRef playerDead, EntityRef entityDead)
		{
			foreach (var collectable in f.Unsafe.GetComponentBlockIterator<Collectable>())
			{
				StopCollecting(f, collectable.Entity, entityDead, playerDead, collectable.Component);
			}
		}

		private void StartCollecting(Frame f, PlayerRef player, EntityRef playerEntity, Collectable* collectable,
		                             EntityRef collectableEntity)
		{
			collectable->CollectorsEndTime[player] = GetEndTime(f, collectableEntity, playerEntity);
			f.Events.OnStartedCollecting(collectableEntity, *collectable, player, playerEntity);
		}

		private void StopCollecting(Frame f, EntityRef entity, EntityRef playerEntity, PlayerRef player,
		                            Collectable* collectable)
		{
			if (!collectable->IsCollecting(player))
			{
				return;
			}

			collectable->CollectorsEndTime[player] = FP._0;

			f.Events.OnStoppedCollecting(entity, player, playerEntity);
		}

		private void Collect(Frame f, EntityRef entity, EntityRef playerEntity, PlayerRef player,
		                     Collectable* collectable)
		{
			var gameId = collectable->GameId;

			if (f.Unsafe.TryGetPointer<EquipmentCollectable>(entity, out var equipment))
			{
				var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
				if (playerCharacter->HasBetterWeaponEquipped(equipment->Item))
				{
					gameId = GameId.AmmoSmall;
					var weaponConfig = f.WeaponConfigs.GetConfig(equipment->Item.GameId);
					var initialAmmo = weaponConfig.InitialAmmoFilled.Get(f);
					var consumable = new Consumable {ConsumableType = ConsumableType.Ammo, Amount = initialAmmo};
					var ammoWasEmpty = playerCharacter->GetAmmoAmountFilled(f, playerEntity) < FP.SmallestNonZero;

					// Fake use a consumable to simulate it's natural life cycle
					f.Add(entity, consumable);
					consumable.Collect(f, entity, playerEntity, player);

					// Special case: having no ammo and collecting the weapon that is already in one of the slots
					if (ammoWasEmpty && playerCharacter->HasMeleeWeapon(f, playerEntity))
					{
						playerCharacter->TryEquipExistingWeaponID(f, playerEntity, equipment->Item.GameId);
					}
				}
				else
				{
					equipment->Collect(f, entity, playerEntity, player);
				}
			}
			else if (f.Unsafe.TryGetPointer<Consumable>(entity, out var consumable))
			{
				consumable->Collect(f, entity, playerEntity, player);
			}
			else if (f.Unsafe.TryGetPointer<Chest>(entity, out var chest))
			{
				chest->Open(f, entity, playerEntity, player);
				if (f.TryGet<AirDrop>(entity, out var airDrop))
				{
					f.Events.OnAirDropCollected(entity, playerEntity, airDrop);
				}
			}
			else
			{
				throw new NotSupportedException($"Trying to collect an unsupported / missing collectable on {entity}.");
			}

			f.Events.OnCollectableCollected(gameId, entity, player, playerEntity);
		}

		private FP GetEndTime(Frame f, EntityRef consumableEntity, EntityRef playerEntity)
		{
			var timeMod = f.Get<Stats>(playerEntity).GetStatData(StatType.PickupSpeed).StatValue;

			// If it's a consumable then we use CollectTime from consumable config
			if (f.TryGet<Consumable>(consumableEntity, out var consumable))
			{
				return f.Time + FPMath.Max((consumable.CollectTime - timeMod), Constants.PICKUP_SPEED_MINIMUM);
			}

			// Otherwise we use global collect time
			return f.Time + FPMath.Max(f.GameConfig.CollectableCollectTime.Get(f) - timeMod,
			                           Constants.PICKUP_SPEED_MINIMUM);
		}
	}
}