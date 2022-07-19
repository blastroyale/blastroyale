using System;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the <see cref="Collectable"/> component collection interactions using triggers 
	/// </summary>
	public unsafe class CollectableSystem : SystemSignalsOnly, ISignalHealthIsZero,
	                                        ISignalOnComponentRemoved<PlayerCharacter>,
	                                        ISignalOnTriggerEnter3D, ISignalOnTrigger3D, ISignalOnTriggerExit3D
	{
		public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
		{
			if (!f.Unsafe.TryGetPointer<Collectable>(info.Entity, out var collectable) ||
			    !f.Has<AlivePlayerCharacter>(info.Other) || !f.TryGet<PlayerCharacter>(info.Other, out var player))
			{
				return;
			}

			if (IsCollectableFilled(f, info.Entity, info.Other))
			{
				f.Events.OnLocalCollectableBlocked(collectable->GameId, info.Entity, player.Player, info.Other);
			}
		}

		public void OnTrigger3D(Frame f, TriggerInfo3D info)
		{
			if (!f.Unsafe.TryGetPointer<Collectable>(info.Entity, out var collectable) ||
			    !f.Has<AlivePlayerCharacter>(info.Other) || !f.TryGet<PlayerCharacter>(info.Other, out var player))
			{
				return;
			}

			if (collectable->IsCollected)
			{
				f.Add<EntityDestroyer>(info.Entity);
				return;
			}

			// If you are full on the stat the collectable is attempting to refill, then do not collect
			if (IsCollectableFilled(f, info.Entity, info.Other)) return;

			var endTime = collectable->CollectorsEndTime[player.Player];

			if (!collectable->IsCollecting(player.Player))
			{
				// If it's a consumable then we use CollectTime from consumable config
				if (f.TryGet<Consumable>(info.Entity, out var consumable))
				{
					endTime = f.Time + consumable.CollectTime;
				}
				// Otherwise we use global collect time
				else
				{
					endTime = f.Time + f.GameConfig.CollectableCollectTime.Get(f);
				}

				collectable->CollectorsEndTime[player.Player] = endTime;

				f.Events.OnLocalStartedCollecting(info.Entity, *collectable, player.Player, info.Other);
			}

			if (f.Time < endTime)
			{
				return;
			}

			collectable->IsCollected = true;

			Collect(f, info.Entity, info.Other, player.Player, collectable);
		}

		public void OnTriggerExit3D(Frame f, ExitInfo3D info)
		{
			if (!f.Unsafe.TryGetPointer<Collectable>(info.Entity, out var collectable) ||
			    !f.TryGet<PlayerCharacter>(info.Other, out var player))
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
						       stats.GetStatData(StatType.Shield).StatValue;
					case ConsumableType.Ammo:
						return playerCharacter.GetAmmoAmountFilled(f, player) == 1;
				}
			}

			return false;
		}

		/// <inheritdoc />
		public void HealthIsZero(Frame f, EntityRef entity, EntityRef attacker)
		{
			if (!f.TryGet<PlayerCharacter>(entity, out var playerCharacter))
			{
				return;
			}

			foreach (var collectable in f.Unsafe.GetComponentBlockIterator<Collectable>())
			{
				StopCollecting(f, collectable.Entity, entity, playerCharacter.Player, collectable.Component);
			}
		}

		/// <inheritdoc />
		public void OnRemoved(Frame f, EntityRef entity, PlayerCharacter* component)
		{
			foreach (var collectable in f.Unsafe.GetComponentBlockIterator<Collectable>())
			{
				StopCollecting(f, collectable.Entity, entity, component->Player, collectable.Component);
			}
		}

		private void StopCollecting(Frame f, EntityRef entity, EntityRef playerEntity, PlayerRef player,
		                            Collectable* collectable)
		{
			if (!collectable->IsCollecting(player))
			{
				return;
			}

			collectable->CollectorsEndTime[player] = FP._0;

			f.Events.OnLocalStoppedCollecting(entity, player, playerEntity);
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
					var ammoAmount = f.ConsumableConfigs.GetConfig(gameId).Amount;
					playerCharacter->GainAmmo(f, playerEntity, ammoAmount);
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

			f.Events.OnLocalCollectableCollected(gameId, entity, player, playerEntity);
			f.Events.OnCollectableCollected(gameId, entity, player, playerEntity);
		}
	}
}