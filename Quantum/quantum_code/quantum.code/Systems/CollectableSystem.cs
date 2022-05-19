using System;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the <see cref="Collectable"/> component collection interactions using triggers 
	/// </summary>
	public unsafe class CollectableSystem : SystemSignalsOnly, ISignalHealthIsZero,
	                                        ISignalOnComponentRemoved<PlayerCharacter>,
	                                        ISignalOnTrigger3D, ISignalOnTriggerExit3D
	{
		/// <inheritdoc />
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
					endTime = f.Time + f.GameConfig.CollectableCollectTime;
				}

				collectable->CollectorsEndTime[player.Player] = endTime;

				f.Events.OnLocalStartedCollecting(info.Entity, *collectable, player.Player, info.Other);
			}

			if (f.Time < endTime)
			{
				return;
			}

			collectable->IsCollected = true;

			Collect(f, info.Entity, info.Other, player.Player);
			f.Events.OnLocalCollectableCollected(collectable->GameId, info.Entity, player.Player, info.Other);
			f.Events.OnCollectableCollected(collectable->GameId, info.Entity, player.Player, info.Other);
		}

		/// <inheritdoc />
		public void OnTriggerExit3D(Frame f, ExitInfo3D info)
		{
			if (!f.Unsafe.TryGetPointer<Collectable>(info.Entity, out var collectable) ||
			    !f.TryGet<PlayerCharacter>(info.Other, out var player))
			{
				return;
			}

			StopCollecting(f, info.Entity, info.Other, player.Player, collectable);
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

		private void Collect(Frame f, EntityRef entity, EntityRef playerEntity, PlayerRef player)
		{
			if (f.Unsafe.TryGetPointer<WeaponCollectable>(entity, out var weapon))
			{
				weapon->Collect(f, entity, playerEntity);
			}
			else if (f.Unsafe.TryGetPointer<Consumable>(entity, out var consumable))
			{
				consumable->Collect(f, entity, playerEntity, player);
				f.Events.OnConsumablePicked(entity, *consumable, player, playerEntity);
			}
			else if (f.Unsafe.TryGetPointer<Chest>(entity, out var chest))
			{
				chest->Open(f, entity, playerEntity);
			}
			else
			{
				throw new NotSupportedException($"Trying to collect an unsupported / missing collectable on {entity}.");
			}
		}
	}
}