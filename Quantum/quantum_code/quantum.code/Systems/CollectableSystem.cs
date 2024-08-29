using System;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the <see cref="Collectable"/> component collection interactions using triggers 
	/// </summary>
	public unsafe class CollectableSystem : SystemSignalsOnly, ISignalPlayerDead,
											ISignalOnFeetCollisionEnter, ISignalOnFeetCollisionContinue, ISignalOnFeetCollisionLeft,
											ISignalPlayerColliderDisabled
	{
		
		public void OnFeetCollisionEnter(Frame f, EntityRef entity, EntityRef collidedWith, FPVector2 point)
		{
			TryStartCollecting(f, collidedWith, entity, true);
		}

		public void OnFeetCollisionContinue(Frame f, EntityRef collector, EntityRef collidedWith)
		{
			if (!f.Unsafe.TryGetPointer<Collectable>(collidedWith, out var collectable) ||
				!f.Has<AlivePlayerCharacter>(collector) || !f.Unsafe.TryGetPointer<PlayerCharacter>(collector, out var player) ||
				f.Has<EntityDestroyer>(collidedWith))
			{
				return;
			}

			var playerEntity = collector;

			// We try to start collecting here because collectable may be allowed to
			// become collected after it already triggered with a player
			if (!collectable->IsCollecting(f, collidedWith, playerEntity))
			{
				if (!TryStartCollecting(f, collidedWith, collector, false))
				{
					return;
				}
			}

			if (!collectable->TryGetCollectingEndTime(f, collidedWith, playerEntity, out var endTime) || f.Time < endTime)
			{
				return;
			}

			if (IsCollectableFilled(f, collidedWith, collector) || ReviveSystem.IsKnockedOut(f, collector))
			{
				//f.Events.OnCollectableBlocked(collectable->GameId, info.Entity, player.Player, info.Other);
				StopCollecting(f, collidedWith, collector, player->Player, collectable);
				return;
			}

			Collect(f, collidedWith, collector, player->Player, collectable);

			f.Destroy(collidedWith);
		}

		public void OnFeetCollisionLeft(Frame f, EntityRef collector, EntityRef collidedWith)
		{
			if (!f.Unsafe.TryGetPointer<Collectable>(collidedWith, out var collectable) ||
				!f.Has<AlivePlayerCharacter>(collector) || !f.Unsafe.TryGetPointer<PlayerCharacter>(collector, out var player) ||
				f.Has<EntityDestroyer>(collidedWith))
			{
				return;
			}
			StopCollecting(f, collidedWith, collector, player->Player, collectable);
		}
		
		private bool TryStartCollecting(Frame f, EntityRef entity, EntityRef collector, bool sendEvent)
		{
			if (!f.Unsafe.TryGetPointer<Collectable>(entity, out var collectable) ||
				!GameContainer.HasGameStarted(f) ||
				!f.Has<AlivePlayerCharacter>(collector) ||
				!f.Unsafe.TryGetPointer<PlayerCharacter>(collector, out var player) || f.Has<EntityDestroyer>(entity) ||
				ReviveSystem.IsKnockedOut(f, collector))
			{
				return false;
			}

			if (IsCollectableFilled(f, entity, collector))
			{
				if (sendEvent)
				{
					f.Events.OnCollectableBlocked(collectable->GameId, entity, player->Player, collector);
				}
				return false;
			}

			return StartCollecting(f, player->Player, collector, collectable, entity);
		}

		private bool IsCollectableFilled(Frame f, EntityRef entity, EntityRef player)
		{
			if (f.Unsafe.TryGetPointer<Consumable>(entity, out var consumable))
			{
				if (consumable->ConsumableType == ConsumableType.Special)
				{
					return !f.Unsafe.GetPointer<PlayerInventory>(player)->HasSpaceForSpecial();
				}

				var stats = f.Unsafe.GetPointer<Stats>(player);
				return stats->IsConsumableStatFilled(consumable->ConsumableType);
			}

			return false;
		}

		public void PlayerDead(Frame f, PlayerRef playerDead, EntityRef entityDead)
		{
			// TODO: Optimize
			foreach (var collectable in f.Unsafe.GetComponentBlockIterator<Collectable>())
			{
				StopCollecting(f, collectable.Entity, entityDead, playerDead, collectable.Component);
			}
		}

		private bool StartCollecting(Frame f, PlayerRef player, EntityRef collector, Collectable* collectable,
									 EntityRef collectableEntity)
		{
			if (collectable->IsCollecting(f, collectableEntity, collector)) return false;
			collectable->StartCollecting(f, collectableEntity, collector, GetCollectDuration(f, collectableEntity, collector));
			f.Events.OnStartedCollecting(collectableEntity, *collectable, collector);

			return true;
		}

		/// <summary>
		/// Hack until consumables, collectibesl, equipment collectibes and all this mess is centralized
		/// </summary>
		public static FP GetCollectionRadius(Frame frame, GameId id)
		{
			var config = frame.ConsumableConfigs.GetConfig(id);
			if (config != null) return config.CollectableConsumablePickupRadius;
			if (frame.ChestConfigs.HasConfig(id))
			{
				return frame.ChestConfigs.GetConfig(id).CollectableChestPickupRadius;
			}
			return frame.GameConfig.CollectableEquipmentPickupRadius;
		}

		private void StopCollecting(Frame f, EntityRef entity, EntityRef collector, PlayerRef player,
									Collectable* collectable)
		{
			if (!collectable->IsCollecting(f, entity, collector)) return;
			collectable->StopCollecting(f, entity, collector);
			f.Events.OnStoppedCollecting(entity, collector);
		}

		private void Collect(Frame f, EntityRef entity, EntityRef playerEntity, PlayerRef player,
							 Collectable* collectable)
		{
			var gameId = collectable->GameId;

			if (f.Unsafe.TryGetPointer<EquipmentCollectable>(entity, out var equipment))
			{
				var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);

				equipment->Collect(f, entity, playerEntity, player);

				// We restore ammo to initial level if it was lower
				var statsPointer = f.Unsafe.GetPointer<Stats>(playerEntity);
				if (statsPointer->CurrentAmmoPercent < Constants.INITIAL_AMMO_FILLED)
				{
					statsPointer->SetCurrentAmmo(f, playerCharacter, playerEntity, Constants.INITIAL_AMMO_FILLED);
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

			f.Signals.CollectableCollected(gameId, entity, playerEntity, collectable->Spawner);
			f.Events.OnCollectableCollected(gameId, entity, playerEntity, collectable->Spawner,
				f.Unsafe.GetPointer<Transform2D>(entity)->Position);
		}

		private FP GetCollectDuration(Frame f, EntityRef consumableEntity, EntityRef playerEntity)
		{
			var timeMod = f.Unsafe.GetPointer<Stats>(playerEntity)->GetStatData(StatType.PickupSpeed).StatValue;
		
			// We default to global collect time
			var endTime = f.GameConfig.CollectableCollectTime.Get(f);

			// Unless it's a chest or non-equipment consumable in which case we use its collect time
			if (f.TryGet<Chest>(consumableEntity, out var chest))
			{
				endTime = chest.CollectTime;
			}
			else if (f.TryGet<Consumable>(consumableEntity, out var consumable))
			{
				endTime = f.ConsumableConfigs.GetConfig(consumable.ConsumableType).ConsumableCollectTime.Get(f);
			}

			endTime = FPMath.Max(Constants.PICKUP_SPEED_MINIMUM, (FP._1 - (timeMod / FP._100)) * endTime);

			return endTime;
		}

		public void PlayerColliderDisabled(Frame f, EntityRef playerEntity)
		{
			if (!f.Unsafe.TryGetPointer<PlayerCharacter>(playerEntity, out var player))
			{
				return;
			}

			foreach (var collectable in f.Unsafe.GetComponentBlockIterator<Collectable>())
			{
				StopCollecting(f, collectable.Entity, playerEntity, player->Player, collectable.Component);
			}
		}
	}
}