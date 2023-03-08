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
						return FPMath.CeilToInt(playerCharacter.GetAmmoAmountFilled(f, player) * 100) == 100;
					case ConsumableType.Exp:
						return playerCharacter.CurrentLevel == f.GameConfig.MaxPlayerLevel;
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
			if (collectable->IsCollecting(player)) return;

			collectable->CollectorsEndTime[player] = GetEndTime(f, collectableEntity, playerEntity);
			f.Events.OnStartedCollecting(collectableEntity, *collectable, player, playerEntity);
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

				if (!f.Has<BotCharacter>(playerEntity))
				{
					var loadoutMetadata = playerCharacter->GetLoadoutMetadata(f, equipment->Item);
					
					// We count how many NFTs from their loadout a player has collected to use later for CS earnings
					if (loadoutMetadata != null && loadoutMetadata.Value.IsNft)
					{
						// TODO: Handle a situation when a player somehow collects not his Helmet first but then collects
						// his NFT Helmet instead. Current logic will NOT do increment in this edge case
						
						var slotIsBusy = equipment->Item.IsWeapon() ?
											 playerCharacter->WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon.IsValid() :
											 playerCharacter->Gear[PlayerCharacter.GetGearSlot(equipment->Item)].IsValid();
						
						if (!slotIsBusy)
						{
							var playerData = f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData;
							var matchData = playerData[player];
							
							// TODO: This code duplicates the struct every time we use it. Needs refactoring
							matchData.CollectedOwnedNfts++;
							
							// We have to do reassign to store the updated value
							playerData[player] = matchData;
						}
					}
				}
			
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
						playerCharacter->TryEquipExistingWeaponId(f, playerEntity, equipment->Item.GameId);
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

			f.Signals.CollectableCollected(gameId, entity, player, playerEntity, collectable->Spawner);
			f.Events.OnCollectableCollected(gameId, entity, player, playerEntity, collectable->Spawner);
		}

		private FP GetEndTime(Frame f, EntityRef consumableEntity, EntityRef playerEntity)
		{
			var timeMod = f.Get<Stats>(playerEntity).GetStatData(StatType.PickupSpeed).StatValue;

			// We default to global collect time
			var endTime = f.GameConfig.CollectableCollectTime.Get(f);

			// Unless it's a consumable in which case we use it's collect time
			if (f.TryGet<Consumable>(consumableEntity, out var consumable))
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