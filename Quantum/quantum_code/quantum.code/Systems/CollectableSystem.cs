using System;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the <see cref="Collectable"/> component collection interactions using triggers 
	/// </summary>
	public unsafe class CollectableSystem : SystemSignalsOnly, ISignalPlayerDead,
											ISignalOnTriggerEnter3D, ISignalOnTrigger3D, ISignalOnTriggerExit3D,
											ISignalPlayerColliderDisabled
	{
		public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
		{
			TryStartCollecting(f, info, true);
		}

		public void OnTrigger3D(Frame f, TriggerInfo3D info)
		{
			if (!f.Unsafe.TryGetPointer<Collectable>(info.Entity, out var collectable) ||
				!f.Has<AlivePlayerCharacter>(info.Other) || !f.TryGet<PlayerCharacter>(info.Other, out var player) ||
				f.Has<EntityDestroyer>(info.Entity))
			{
				return;
			}

			// We try to start collecting here because collectable may be allowed to
			// become collected after it already triggered with a player
			if (!collectable->IsCollecting(player.Player) && f.Time >= collectable->AllowedToPickupTime)
			{
				if (!TryStartCollecting(f, info, false))
				{
					return;
				}
			}

			var endTime = collectable->CollectorsEndTime[player.Player];
			if (endTime == FP._0 || f.Time < endTime)
			{
				return;
			}

			if (IsCollectableFilled(f, info.Entity, info.Other) || ReviveSystem.IsKnockedOut(f, info.Other))
			{
				//f.Events.OnCollectableBlocked(collectable->GameId, info.Entity, player.Player, info.Other);
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

		private bool TryStartCollecting(Frame f, TriggerInfo3D info, bool sendEvent)
		{
			if (!f.Unsafe.TryGetPointer<Collectable>(info.Entity, out var collectable) ||
				f.Time < collectable->AllowedToPickupTime || !f.Has<AlivePlayerCharacter>(info.Other) ||
				!f.TryGet<PlayerCharacter>(info.Other, out var player) || f.Has<EntityDestroyer>(info.Entity) || ReviveSystem.IsKnockedOut(f, info.Other))
			{
				return false;
			}

			if (IsCollectableFilled(f, info.Entity, info.Other))
			{
				if (sendEvent)
				{
					f.Events.OnCollectableBlocked(collectable->GameId, info.Entity, player.Player, info.Other);
				}

				return false;
			}

			return StartCollecting(f, player.Player, info.Other, collectable, info.Entity);
		}

		private bool IsCollectableFilled(Frame f, EntityRef entity, EntityRef player)
		{
			if (f.Unsafe.TryGetPointer<Consumable>(entity, out var consumable))
			{
				if (consumable->ConsumableType == ConsumableType.Special)
				{
					return !f.Unsafe.GetPointer<PlayerInventory>(player)->HasSpaceForSpecial();
				}

				var stats = f.Get<Stats>(player);
				return stats.IsConsumableStatFilled(consumable->ConsumableType);
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

		private bool StartCollecting(Frame f, PlayerRef player, EntityRef playerEntity, Collectable* collectable,
									 EntityRef collectableEntity)
		{
			if (collectable->IsCollecting(player)) return false;

			collectable->CollectorsEndTime[player] = GetEndTime(f, collectableEntity, playerEntity);
			f.Events.OnStartedCollecting(collectableEntity, *collectable, player, playerEntity);

			return true;
		}

		private void StopCollecting(Frame f, EntityRef entity, EntityRef playerEntity, PlayerRef player,
									Collectable* collectable)
		{
			if (!collectable->IsCollecting(player)) return;

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

				if (playerCharacter->HasBetterWeaponEquipped(&equipment->Item))
				{
					gameId = GameId.AmmoSmall;
					var stats = f.Get<Stats>(playerEntity);
					var ammoSmallConfig = f.ConsumableConfigs.GetConfig(GameId.AmmoSmall);
					var initialAmmo = ammoSmallConfig.Amount.Get(f);
					var consumable = new Consumable { ConsumableType = ConsumableType.Ammo, Amount = initialAmmo };
					var ammoWasEmpty = stats.CurrentAmmoPercent < FP.SmallestNonZero;

					// Fake use a consumable to simulate it's natural life cycle
					f.Add(entity, consumable);
					consumable.Collect(f, entity, playerEntity, player);

					// Special case: having no ammo and collecting the weapon that is already in one of the slots
					if (ammoWasEmpty && playerCharacter->HasMeleeWeapon(f, playerEntity))
					{
						playerCharacter->TryEquipExistingWeaponId(f, playerEntity, equipment->Item.GameId);
					}
				}
				else
				{
					equipment->Collect(f, entity, playerEntity, player);

					// In Looting 2.0 we restore ammo to initial level if it was lower in a previous gun
					if (f.Context.MapConfig.LootingVersion == 2 && equipment->Item.IsWeapon())
					{
						var statsPointer = f.Unsafe.GetPointer<Stats>(playerEntity);
						if (statsPointer->CurrentAmmoPercent < Constants.INITIAL_AMMO_FILLED)
						{
							statsPointer->SetCurrentAmmo(f, playerCharacter, playerEntity, Constants.INITIAL_AMMO_FILLED);
						}
					}
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

			f.Signals.CollectableCollected(gameId, entity, player, playerEntity, collectable->Spawner);
			f.Events.OnCollectableCollected(gameId, entity, player, playerEntity, collectable->Spawner,
				f.Get<Transform3D>(entity).Position);
		}

		private FP GetEndTime(Frame f, EntityRef consumableEntity, EntityRef playerEntity)
		{
			var timeMod = f.Get<Stats>(playerEntity).GetStatData(StatType.PickupSpeed).StatValue;

			// We default to global collect time
			var endTime = f.GameConfig.CollectableCollectTime.Get(f);

			// Unless it's a chest or non-equipment consumable in which case we use its collect time
			if (f.TryGet<Chest>(consumableEntity, out var chest))
			{
				endTime = chest.CollectTime;
			}
			else if (f.TryGet<Consumable>(consumableEntity, out var consumable))
			{
				endTime = consumable.CollectTime;
			}

			endTime = FPMath.Max(Constants.PICKUP_SPEED_MINIMUM, (FP._1 - (timeMod / FP._100)) * endTime);

			return f.Time + endTime;
		}

		public void PlayerColliderDisabled(Frame f, EntityRef playerEntity)
		{
			if (!f.TryGet<PlayerCharacter>(playerEntity, out var player))
			{
				return;
			}

			foreach (var collectable in f.Unsafe.GetComponentBlockIterator<Collectable>())
			{
				StopCollecting(f, collectable.Entity, playerEntity, player.Player, collectable.Component);
			}
		}
	}
}