using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;

namespace Quantum.Systems.Bots
{
	public unsafe static class BotPickups
	{
		private static FP RealCloseItemDistanceSquared = FP._7 * FP._7;

		public struct CollectibleFilter
		{
			public EntityRef Entity;
			public Collectable* Component;
		}

		private static bool IsInVisionRange(FP distanceSqr, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			var visionRangeSqr = filter.BotCharacter->VisionRangeSqr;
			return visionRangeSqr < FP._0 || distanceSqr <= visionRangeSqr;
		}

		/// <summary>
		/// Checks if a bot is currently going towards a valid collectable.
		/// If ti is it will return true and the collectable instance
		/// </summary>
		public static bool IsGoingTowardsCollectible(this ref BotCharacterSystem.BotCharacterFilter filter)
		{
			return filter.BotCharacter->MoveTarget.IsValid && filter.BotCharacter->MoveTarget != filter.Entity;
		}

		/// <summary>
		/// Checks if a bot is currently going towards a valid collectable.
		/// If ti is it will return true and the collectable instance
		/// </summary>
		public static bool IsGoingTowardsValidCollectible(this ref BotCharacterSystem.BotCharacterFilter filter, Frame f, out Collectable* collectable)
		{
			if (filter.IsGoingTowardsCollectible())
			{
				return f.Unsafe.TryGetPointer(filter.BotCharacter->MoveTarget, out collectable);
			}

			collectable = default;
			return false;
		}


		private static IEnumerable<short> ChunksEnumerator(Frame f, short botChunk)
		{
			yield return botChunk;
			yield return CollectableChunkSystem.AddChunks(f, botChunk, -1, 0);
			yield return CollectableChunkSystem.AddChunks(f, botChunk, -1, 1);
			yield return CollectableChunkSystem.AddChunks(f, botChunk, 0, 1);
			yield return CollectableChunkSystem.AddChunks(f, botChunk, 1, 1);
			yield return CollectableChunkSystem.AddChunks(f, botChunk, 1, 0);
			yield return CollectableChunkSystem.AddChunks(f, botChunk, 1, -1);
			yield return CollectableChunkSystem.AddChunks(f, botChunk, 0, -1);
			yield return CollectableChunkSystem.AddChunks(f, botChunk, -1, -1);
		}

		private enum PickupType
		{
			Weapon,
			GoldenWeapon,
			FavoriteWeapon,
			Ammo,
			Health,
			Shield,
			Special,
			Chest,
			LegendaryChest
		}

		private struct FindCollectableContext
		{
			private Dictionary<PickupType, (EntityRef Entity, FP Distance, GameId Id)> _items;
			private Dictionary<PickupType, (EntityRef Entity, FP Distance, GameId Id)> _realClose;
			private BotCharacterSystem.BotCharacterFilter _filter;
			private Frame _f;

			public void Init(Frame f, BotCharacterSystem.BotCharacterFilter filter)
			{
				this._f = f;
				this._filter = filter;
				_items = new Dictionary<PickupType, (EntityRef, FP, GameId)>();
				_realClose = new Dictionary<PickupType, (EntityRef, FP, GameId)>();
				foreach (var value in Enum.GetValues(typeof(PickupType)).Cast<PickupType>())
				{
					_items[value] = (EntityRef.None, FP.MaxValue, GameId.Random);
					_realClose[value] = (EntityRef.None, FP.MaxValue, GameId.Random);
				}
			}

			public void CheckSet(PickupType type, EntityRef entityRef, GameId gameId, FP distance)

			{
				if (_items.TryGetValue(type, out var currentValue))
				{
					if (distance < currentValue.Distance)
					{
						_items[type] = (entityRef, distance, gameId);
					}
				}

				if (distance < RealCloseItemDistanceSquared)
				{
					if (_realClose.TryGetValue(type, out var closeItem))
					{
						if (distance < closeItem.Distance)
						{
							_realClose[type] = (entityRef, distance, gameId);
						}
					}
				}
			}

			private Dictionary<PickupType, (EntityRef Entity, FP Distance, GameId Id)> GetDict(bool realClose)
			{
				return realClose ? _realClose : _items;
			}

			public bool Found(PickupType pickupType, bool realClose = false)
			{
				var item = GetDict(realClose)[pickupType];
				return item.Entity.IsValid;
			}

			public bool FoundAndNeed(PickupType type, bool realClose = false)
			{
				return Found(type, realClose) && Need(type, realClose);
			}


			private bool NoAmmo()
			{
				var stats = _f.Unsafe.GetPointer<Stats>(_filter.Entity);
				var currentAmmo = stats->CurrentAmmoPercent;
				return currentAmmo < FP.SmallestNonZero;
			}

			private bool HasAmmo(FP pct)
			{
				var stats = _f.Unsafe.GetPointer<Stats>(_filter.Entity);
				var currentAmmo = stats->CurrentAmmoPercent;
				return currentAmmo >= pct;
			}

			private bool HasWeapon()
			{
				var main = _filter.PlayerCharacter->WeaponSlots[Constants.WEAPON_INDEX_PRIMARY];
				return main.Weapon.IsValid() && main.Weapon.IsWeapon();
			}

			private bool HasFavoriteWeapon()
			{
				var main = _filter.PlayerCharacter->WeaponSlots[Constants.WEAPON_INDEX_PRIMARY];
				return main.Weapon.GameId == _filter.BotCharacter->FavoriteWeapon;
			}

			public bool Need(PickupType type, bool close)
			{
				switch (type)
				{
					case PickupType.Weapon:
						return !HasWeapon() || NoAmmo();
					case PickupType.GoldenWeapon:
						// Bots prefer favorite weapon then golden one
						if (GetDict(close)[PickupType.GoldenWeapon].Id == _filter.BotCharacter->FavoriteWeapon)
						{
							return HasFavoriteWeapon() && !_filter.PlayerCharacter->HasGoldenWeapon();
						}

						return !_filter.PlayerCharacter->HasGoldenWeapon() && !HasFavoriteWeapon();
					case PickupType.FavoriteWeapon:
						// The nearest golden weapon may not be the favorite, but the favorite one may be golden
						var fav = GetDict(close)[PickupType.FavoriteWeapon];
						if (fav.Entity.IsValid && _f.Unsafe.TryGetPointer<EquipmentCollectable>(fav.Entity, out var eq) && eq->Item.Material == EquipmentMaterial.Golden)
						{
							return !_filter.PlayerCharacter->HasGoldenWeapon();
						}

						return !HasWeapon() || !HasFavoriteWeapon();
					case PickupType.Ammo:
						return !HasAmmo(close ? FP._0_99 : FP._0_50);
					case PickupType.Health:
						var healthStats = _f.Unsafe.GetPointer<Stats>(_filter.Entity);
						var maxHealth = healthStats->Values[(int)StatType.Health].StatValue;
						return healthStats->CurrentHealth < maxHealth;
					case PickupType.Shield:
						var shieldStats = _f.Unsafe.GetPointer<Stats>(_filter.Entity);
						var maxShields = shieldStats->Values[(int)StatType.Shield].StatValue;
						return shieldStats->CurrentShield < maxShields;
					case PickupType.Special:
						return _filter.PlayerInventory->HasSpaceForSpecial();
					case PickupType.Chest:
						return true;
					case PickupType.LegendaryChest:
						return true;
				}

				return false;
			}

			private class PickupPriority
			{
				public PickupType[] PickupTypes;

				public PickupPriority(params PickupType[] pickupTypes)
				{
					PickupTypes = pickupTypes;
				}
			}

			public (EntityRef Entity, GameId Id, bool Close) Choose()
			{
				var priorities = new[]
				{
					new PickupPriority(PickupType.FavoriteWeapon, PickupType.LegendaryChest, PickupType.GoldenWeapon),
					new PickupPriority(PickupType.Weapon),
					new PickupPriority(PickupType.Health, PickupType.Shield, PickupType.Chest),
					new PickupPriority(PickupType.Special, PickupType.Ammo),
				};
				foreach (var close in new bool[] { true, false })
				{
					BotLogger.LogAction(_f, _filter.Entity, $"Options{(close ? " Close" : "")}: {string.Join("\n", GetDict(close).Where(kv => kv.Value.Entity.IsValid).Select(op => $"{op.Key}({op.Value.Id.ToString()})-{op.Value.Distance.ToString("F")}"))}");
					foreach (var priority in priorities)
					{
						var context = this;
						var closest = GetDict(close)
							.Where(kv => kv.Value.Entity.IsValid && priority.PickupTypes.Contains(kv.Key))
							.OrderBy(kv => kv.Value.Distance)
							.FirstOrDefault(kv => context.FoundAndNeed(kv.Key, close));

						if (closest.Value.Entity.IsValid)
						{
							return (closest.Value.Entity, closest.Value.Id, close);
						}
					}
				}

				return (EntityRef.None, GameId.Random, false);
			}
		}

		public static bool TryGoForClosestCollectable(this ref BotCharacterSystem.BotCharacterFilter filter, Frame f, in BotUpdateGlobalContext botCtx)
		{
			var botPosition = filter.Transform->Position;
			var teamMembers = TeamSystem.GetTeamMemberEntities(f, filter.Entity);
			var invalidTargets = f.ResolveHashSet(filter.BotCharacter->InvalidMoveTargets);

			var botChunk = CollectableChunkSystem.GetChunk(f, botPosition);

			var ctx = new FindCollectableContext();
			ctx.Init(f, filter);

			foreach (var chunk in ChunksEnumerator(f, botChunk))
			{
				var collectables = CollectableChunkSystem.GetCollectables(f, chunk);

				foreach (var (collectableEntity, collectable) in collectables)
				{
					if (invalidTargets.Contains(collectableEntity))
					{
						continue;
					}

					var teamCollecting = false;

					if (collectable->HasCollector(f, collectableEntity, teamMembers))
					{
						continue;
					}

					// If team mate is collecting ignore it!
					foreach (var member in teamMembers)
					{
						if (f.TryGet<BotCharacter>(member, out var otherBot))
						{
							if (otherBot.MoveTarget == collectableEntity)
							{
								teamCollecting = true;
								break;
							}
						}
					}

					if (teamCollecting)
					{
						continue;
					}

					var collectablePosition = collectableEntity.GetPosition(f);
					var distanceToPlayer = FPVector2.DistanceSquared(collectablePosition, botPosition);

					void CheckSet(PickupType pickupType)
					{
						ctx.CheckSet(pickupType, collectableEntity, collectable->GameId, distanceToPlayer);
					}


					if (BotState.IsPositionSafe(botCtx, filter, collectablePosition))
					{
						if (f.Unsafe.TryGetPointer<Consumable>(collectableEntity, out var consumable))
						{
							switch (consumable->ConsumableType)
							{
								case ConsumableType.Ammo:
									CheckSet(PickupType.Ammo);
									break;
								case ConsumableType.Health:
									CheckSet(PickupType.Health);
									break;
								case ConsumableType.Shield:
									CheckSet(PickupType.Shield);
									break;
								case ConsumableType.Special:
									CheckSet(PickupType.Special);
									break;
								default:
									continue;
							}
						}

						if (f.Unsafe.TryGetPointer<EquipmentCollectable>(collectableEntity, out var eq))
						{
							CheckSet(PickupType.Weapon);
							if (eq->Item.IsWeapon() && eq->Item.Material == EquipmentMaterial.Golden)
							{
								CheckSet(PickupType.GoldenWeapon);
							}

							if (collectable->GameId == filter.BotCharacter->FavoriteWeapon)
							{
								CheckSet(PickupType.FavoriteWeapon);
							}
						}

						if (collectable->GameId == GameId.ChestEquipment)
						{
							CheckSet(PickupType.Chest);
						}

						if (collectable->GameId == GameId.ChestLegendary)
						{
							CheckSet(PickupType.LegendaryChest);
						}
					}
				}
			}


			var finalTarget = ctx.Choose();
			if (!finalTarget.Entity.IsValid)
			{
				return false;
			}

			if (filter.NavMeshAgent->IsActive
				&& filter.BotCharacter->MoveTarget == finalTarget.Entity)
			{
				BotLogger.LogAction(f, ref filter, "waiting arriving at consumable");
				return true;
			}

			var pos = f.Unsafe.GetPointer<Transform2D>(finalTarget.Entity)->Position;
			if (BotMovement.MoveToLocation(f, filter.Entity, pos, BotMovementType.GoToCollectable))
			{
				BotLogger.LogAction(f, filter.Entity, $"go collect {(finalTarget.Close ? "close" : "far")} {finalTarget.Id}");
				filter.BotCharacter->MoveTarget = finalTarget.Entity;
				return true;
			}

			return false;
		}
	}
}