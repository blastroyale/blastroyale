using System.Linq;
using Photon.Deterministic;

namespace Quantum.Systems.Bots
{
	public unsafe static class BotPickups
	{
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

		/// <summary>
		/// Checks if a given bot is going towards a collectible that does not exists anymore
		/// </summary>
		public static bool HasInvalidTargetPickup(this ref BotCharacterSystem.BotCharacterFilter filter, Frame f)
		{
			if (filter.IsGoingTowardsCollectible())
			{
				return !filter.IsGoingTowardsValidCollectible(f, out _);
			}

			return false;
		}

// TODO: Implement chunk based spatial positioning

		public static bool TryGoForClosestCollectable(this ref BotCharacterSystem.BotCharacterFilter filter, Frame f, FPVector2 circleCenter, FP circleRadius, bool circleIsShrinking)
		{
			var botPosition = filter.Transform->Position;
			var stats = f.Unsafe.GetPointer<Stats>(filter.Entity);
			var maxShields = stats->Values[(int)StatType.Shield].StatValue;
			var currentAmmo = stats->CurrentAmmoPercent;
			var maxHealth = stats->Values[(int)StatType.Health].StatValue;

			var needWeapon = filter.PlayerCharacter->HasMeleeWeapon(f, filter.Entity) || currentAmmo < FP.SmallestNonZero;
			var needAmmo = currentAmmo < FP._0_99;
			var needShields = stats->CurrentShield < maxShields;
			var needHealth = stats->CurrentHealth < maxHealth;
			var needSpecials = !filter.PlayerInventory->Specials[0].IsUsable(f) ||
				!filter.PlayerInventory->Specials[1].IsUsable(f);

			var teamMembers = TeamSystem.GetTeamMembers(f, filter.Entity);
			var invalidTargets = f.ResolveHashSet(filter.BotCharacter->InvalidMoveTargets);

			var botChunk = CollectableChunkSystem.GetChunk(f, botPosition.XZ);
			var chunks = new[]
			{
				botChunk,
				CollectableChunkSystem.AddChunks(f, botChunk, -1, 0),
				CollectableChunkSystem.AddChunks(f, botChunk, -1, 1),
				CollectableChunkSystem.AddChunks(f, botChunk, 0, 1),
				CollectableChunkSystem.AddChunks(f, botChunk, 1, 1),
				CollectableChunkSystem.AddChunks(f, botChunk, 1, 0),
				CollectableChunkSystem.AddChunks(f, botChunk, 1, -1),
				CollectableChunkSystem.AddChunks(f, botChunk, 0, -1),
				CollectableChunkSystem.AddChunks(f, botChunk, -1, -1)
			};
			var foundItem = false;
			var collectableEntity = EntityRef.None;
			foreach (var chunk in chunks)
			{
				if (foundItem)
				{
					break;
				}

				var collectables = CollectableChunkSystem.GetCollectables(f, chunk);

				foreach (var (entity, collectable) in collectables)
				{
					if (invalidTargets.Contains(entity))
					{
						continue;
					}

					var teamCollecting = false;

					// If team mate is collecting ignore it!
					foreach (var member in teamMembers)
					{
						if (collectable->IsCollecting(f, member.Entity))
						{
							teamCollecting = true;
							break;
						}

						if (f.TryGet<BotCharacter>(member.Entity, out var otherBot))
						{
							if (otherBot.MoveTarget == entity)
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

					if (f.Unsafe.TryGetPointer<Consumable>(entity, out var consumable))
					{
						var usefulConsumable = consumable->ConsumableType switch
						{
							ConsumableType.Ammo    => needAmmo,
							ConsumableType.Shield  => needShields,
							ConsumableType.Health  => needHealth,
							ConsumableType.Special => needSpecials,
							_                      => true
						};

						if (!usefulConsumable)
						{
							continue;
						}
					}


					if (BotState.IsInCircleWithSpareSpace(circleCenter, circleRadius, circleIsShrinking, entity.GetPosition(f)))
					{
						if (collectable->GameId.IsInGroup(GameIdGroup.Weapon))
						{
							if (needWeapon)
							{
								collectableEntity = entity;
								foundItem = true;
								break;
							}

							continue;
						}

						foundItem = !needWeapon;
						collectableEntity = entity;
					}
				}
			}

			if (collectableEntity == EntityRef.None)
			{
				return false;
			}

			if (filter.NavMeshAgent->IsActive
				&& filter.BotCharacter->MoveTarget == collectableEntity)
			{
				return true;
			}

			var pos = f.Unsafe.GetPointer<Transform3D>(collectableEntity)->Position;
			if (BotMovement.MoveToLocation(f, filter.Entity, pos))
			{
				filter.BotCharacter->MoveTarget = collectableEntity;
				return true;
			}

			return false;
		}
	}
}