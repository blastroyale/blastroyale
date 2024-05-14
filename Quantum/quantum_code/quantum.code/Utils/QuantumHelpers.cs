using System;
using System.Collections.Generic;
using System.Diagnostics;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe delegate void SpellCallBack(Frame f, Spell* spell);

	/// <summary>
	/// This class contains various helper functions to use inside Quantum
	/// </summary>
	public static unsafe class QuantumHelpers
	{
		private static readonly FPVector3 LINE_OF_SIGHT_OFFSET = FPVector3.Up / 2;

		/// <summary>
		/// Requests the math <paramref name="power"/> of the given <paramref name="baseValue"/>
		/// </summary>
		public static FP PowFp(FP baseValue, FP power)
		{
			var ret = FP._1;

			for (var i = 0; i < power; i++)
			{
				ret *= baseValue;
			}

			return ret;
		}

		/// <summary>
		/// Makes the given entity <paramref name="e"/> rotate in the XZ axis to the given <paramref name="target"/> position
		/// </summary>
		public static void LookAt2d(Frame f, in EntityRef e, in EntityRef target, in FP lerpTime)
		{
			LookAt2d(f, e, f.Get<Transform3D>(target).Position, lerpTime);
		}

		/// <inheritdoc cref="LookAt2d(Quantum.Frame,Quantum.EntityRef,Quantum.EntityRef)"/>
		public static void LookAt2d(Frame f, in EntityRef e, in FPVector3 target, in FP lerpTime)
		{
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			var direction = target - transform->Position;

			LookAt2d(transform, direction.XZ, lerpTime);
		}

		/// <summary>
		/// Makes the given entity <paramref name="e"/> rotate in the XZ axis in the given <paramref name="direction"/>
		/// </summary>
		public static void LookAt2d(Frame f, in EntityRef e, in FPVector2 direction, in FP lerpTime)
		{
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			LookAt2d(transform, direction, lerpTime);
		}


		public static FPQuaternion ToRotation(this FPVector2 direction)
		{
			return FPQuaternion.AngleAxis(FPMath.Atan2(direction.X, direction.Y) * FP.Rad2Deg, FPVector3.Up);
		}

		public static FPVector2 ToDirection(this FPQuaternion rotation)
		{
			return (rotation * FPVector3.Forward).XZ.Normalized;
		}

		public static bool HasLineOfSight(Frame f, in FPVector3 source, in FPVector3 destination, out EntityRef? firstHit)
		{
			return HasLineOfSight(f, source, destination, f.Context.TargetAllLayerMask, QueryOptions.HitDynamics | QueryOptions.HitStatics |
				QueryOptions.HitKinematics, out firstHit);
		}

		/// <summary>
		/// Checks for map line of sight. Ignores players and other stuff.
		/// </summary>
		public static bool HasMapLineOfSight(Frame f, in EntityRef one, in EntityRef two)
		{
			if (f.Has<Destructible>(two)) return true;
			if (f.TryGet<Transform3D>(one, out var onePosition) && f.TryGet<Transform3D>(two, out var twoPosition))
			{
				return HasLineOfSight(f, onePosition.Position + LINE_OF_SIGHT_OFFSET, twoPosition.Position + LINE_OF_SIGHT_OFFSET, f.Context.TargetMapOnlyLayerMask, QueryOptions.HitStatics, out _);
			}

			return true;
		}

		public static bool HasLineOfSight(Frame f, FPVector3 source, FPVector3 destination, int layerMask, QueryOptions options, out EntityRef? firstHit)
		{
			var hit = f.Physics3D.Linecast(source,
				destination,
				layerMask,
				options
			);
			firstHit = hit?.Entity;
			return !hit.HasValue;
		}

		/// <summary>
		/// Makes the given entity <paramref name="transform"/> rotate in the XZ axis in the given <paramref name="direction"/>
		/// </summary>
		public static void LookAt2d(Transform3D* transform, in FPVector2 direction, in FP lerpAngle)
		{
			var targetAngle = FPMath.Atan2(direction.X, direction.Y) * FP.Rad2Deg;
			if (lerpAngle == FP._0)
			{
				transform->Rotation = FPQuaternion.AngleAxis(targetAngle, FPVector3.Up);
			}

			var currentAngle = transform->Rotation.AsEuler.Y;
			var deltaAngle = FPMath.AngleBetweenDegrees(targetAngle, currentAngle);
			if (FPMath.Abs(deltaAngle) < FP._2)
			{
				transform->Rotation = FPQuaternion.AngleAxis(targetAngle, FPVector3.Up);
				return;
			}

			var diff = FPMath.Abs(deltaAngle);
			var complementDiff = 360 - diff;
			var maxAngleDelta = lerpAngle * (diff < complementDiff ? diff : complementDiff);
			var clampedDeltaAngle = FPMath.Clamp(deltaAngle, -maxAngleDelta, maxAngleDelta);
			transform->Rotation = FPQuaternion.AngleAxis(currentAngle - clampedDeltaAngle, FPVector3.Up);
		}

		/// <summary>
		/// Determines if <paramref name="target"/> entity is between <paramref name="minRange"/> and <paramref name="maxRange"/> of another entity
		/// </summary>
		public static bool IsEntityInRange(Frame f, in EntityRef e, in EntityRef target, in FP minRange, in FP maxRange)
		{
			var sqrDistance = GetDistance(f, e, target);
			return sqrDistance >= (minRange * minRange) && sqrDistance <= (maxRange * maxRange);
		}

		/// <summary>
		/// Determines if <paramref name="target"/> entity is between <paramref name="minRange"/> and <paramref name="maxRange"/> of another entity
		/// </summary>
		public static FP GetDistance(Frame f, in EntityRef e, in EntityRef target)
		{
			return FPVector3.DistanceSquared(f.Unsafe.GetPointer<Transform3D>(target)->Position, f.Unsafe.GetPointer<Transform3D>(e)->Position);
		}

		/// <summary>
		/// Determines if <paramref name="e"/> entity is valid, exists, not marked on destroy and targetable
		/// </summary>
		public static bool IsAttackable(Frame f, EntityRef e, int attackerTeam)
		{
			if (f.Unsafe.GetPointerSingleton<GameContainer>()->IsGameOver)
			{
				return false;
			}

			return !IsDestroyed(f, e) && f.TryGet<Targetable>(e, out var targetable) &&
				(targetable.Team != attackerTeam || targetable.Team == Constants.TEAM_ID_NEUTRAL || attackerTeam == Constants.TEAM_ID_NEUTRAL);
		}

		/// <summary>
		/// Determines if <paramref name="e"/> entity is destroyed in the game
		/// </summary>
		public static bool IsDestroyed(Frame f, EntityRef e)
		{
			return !f.Exists(e) || !e.IsValid || f.Has<EntityDestroyer>(e);
		}

		/// <summary>
		/// Process an AOE attack in the given <paramref name="radius"/> from the given <paramref name="spell"/> to be processed.
		/// On each hit, the <paramref name="onHitCallback"/> will be called.
		/// Return true if at least one hit was successful, false otherwise.
		/// </summary>
		public static uint ProcessAreaHit(Frame f, FP radius, Spell* spell, uint maxHitCount = uint.MaxValue, SpellCallBack onHitCallback = null)
		{
			if (f.Unsafe.GetPointerSingleton<GameContainer>()->IsGameOver)
			{
				return 0;
			}

			uint hitCount = 0;
			var shape = Shape3D.CreateSphere(radius);
			var hits = f.Physics3D.OverlapShape(spell->OriginalHitPosition, FPQuaternion.Identity, shape,
				f.Context.TargetAllLayerMask, QueryOptions.HitDynamics | QueryOptions.HitKinematics);

			hits.SortCastDistance();

			for (var j = 0; j < hits.Count; j++)
			{
				var hitSpell = Spell.CreateInstant(f, hits[j].Entity, spell->Attacker, spell->SpellSource,
					spell->PowerAmount, spell->KnockbackAmount, hits[j].Point, spell->TeamSource);

				if (hitSpell.Victim == spell->Attacker)
				{
					hitSpell.TeamSource = 0;
					//TODO: this self damage modifier should take into account equipment modifiers once we have it, for now it's just a constant
					hitSpell.PowerAmount = (uint)(spell->PowerAmount * Constants.SELF_DAMAGE_MODIFIER);
				}

				if (!ProcessHit(f, &hitSpell))
				{
					continue;
				}

				hitCount++;

				onHitCallback?.Invoke(f, &hitSpell);

				if (hitCount >= maxHitCount)
				{
					break;
				}
			}

			return hitCount;
		}

		/// <summary>
		/// Process a hit source from the given <paramref name="spell"/> to be processed.
		/// Returns true if the hit was successful and false otherwise
		/// </summary>
		public static bool ProcessHit(Frame f, Spell* spell)
		{
			if (!IsAttackable(f, spell->Victim, spell->TeamSource))
			{
				return false;
			}

			if (spell->KnockbackAmount > 0 &&
				f.Unsafe.TryGetPointer<CharacterController3D>(spell->Victim, out var kcc) &&
				f.TryGet<Transform3D>(spell->Victim, out var victimTransform))
			{
				var kick = (victimTransform.Position - spell->OriginalHitPosition).Normalized *
					spell->KnockbackAmount;
				kick.Y = FP._0;
				kcc->Velocity += kick;
			}

			spell->DoHit(f);

			return true;
		}

		/// <summary>
		/// Get a random element from the <see cref="elements"/> based on weights
		/// </summary>
		public static T RngWeightBased<T>(Frame f, int weightSum, List<T> elements, Func<T, int> weightResolver)
		{
			var weight = f.RNG->Next(0, weightSum);

			for (int i = 0, sum = 0; i < elements.Count; i++)
			{
				sum += weightResolver(elements[i]);

				if (weight < sum)
				{
					return elements[i];
				}
			}

			throw new ArgumentOutOfRangeException($"The weight sum {weightSum.ToString()} is bigger than the sumof the " +
				$"weights in the given elements list. " +
				$"Call {nameof(GetWeightSum)} with the same elements first before calling this.");
		}

		/// <summary>
		/// Gets the sum of weights of the given <paramref name="elements"/> list using the given <paramref name="weightResolver"/>
		/// </summary>
		public static int GetWeightSum<T>(List<T> elements, Func<T, int> weightResolver)
		{
			var sum = 0;

			for (var i = 0; i < elements.Count; i++)
			{
				sum += weightResolver(elements[i]);
			}

			return sum;
		}

		/// <summary>
		/// Requests a valid <see cref="Transform3D"/> from one of the <see cref="PlayerSpawner"/> points present in the world.
		/// A spawn point is valid if no one is in it.
		/// If all spawn points are filled with players, then the last point on the list will be picked.
		/// It also activates the return spawn point
		/// It also changes the bot's Behaviour type if the spawn point has ForceStatic
		/// </summary>
		public static EntityComponentPointerPair<Transform3D> GetPlayerSpawnPosition(Frame f, FPVector2 positionToCompare)
		{
			var closest = new EntityComponentPointerPair<PlayerSpawner>();
			var closestDistance = FP._0;

			foreach (var current in f.Unsafe.GetComponentBlockIterator<PlayerSpawner>())
			{
				if (current.Component->SpawnerType != SpawnerType.Player && current.Component->SpawnerType != SpawnerType.Any)
				{
					continue;
				}

				var currentPosition = f.Unsafe.GetPointer<Transform3D>(current.Entity)->Position.XZ;
				var distance = FPVector2.DistanceSquared(currentPosition, positionToCompare);
				if (!closest.Entity.IsValid || distance < closestDistance)
				{
					closest = current;
					closestDistance = distance;
				}
			}
			
			return new EntityComponentPointerPair<Transform3D>
			{
				Component = f.Unsafe.GetPointer<Transform3D>(closest.Entity),
				Entity = closest.Entity
			};
		}

		/// <summary>
		/// Tries to find a closest position on NavMesh to <paramref name="initialPosition"/>
		/// </summary>
		public static bool TryFindPosOnNavMesh(Frame f, FPVector3 initialPosition, out FPVector3 correctedPosition)
		{
			var radius = FP._1_50;
			return TryFindPosOnNavMesh(f, initialPosition, radius, out correctedPosition);
		}

		/// <summary>
		/// Tries to find a random in the circle area with <paramref name="initialPosition"/> and <paramref name="radius"/>
		/// </summary>
		public static bool TryFindPosOnNavMesh(Frame f, FPVector3 initialPosition, FP radius, out FPVector3 correctedPosition)
		{
			var navMesh = f.NavMesh;

			if (navMesh.FindRandomPointOnNavmesh(initialPosition, radius, f.RNG, NavMeshRegionMask.Default,
					out correctedPosition))
			{
				return true;
			}

			if (navMesh.FindClosestTriangle(initialPosition, radius * 2, NavMeshRegionMask.Default, out var triangle,
					out correctedPosition))
			{
				return navMesh.FindRandomPointOnTriangle(triangle, f.RNG, out correctedPosition);
			}

			return false;
		}

		/// <summary>
		/// Returns a random item from <paramref name="items"/>, with equal chance for each.
		/// </summary>
		public static T GetRandomItem<T>(Frame f, params T[] items)
		{
			return items[f.RNG->Next(0, items.Length)];
		}

		/// <summary>
		/// Requests the <see cref="Transform3D"/> position of this <paramref name="entity"/>.
		/// </summary>
		public static FPVector3 GetPosition(this EntityRef entity, Frame f)
		{
			return f.Unsafe.GetPointer<Transform3D>(entity)->Position;
		}

		/// <summary>
		/// Set the Position inside the <see cref="Transform3D"/> component of the provided entity
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="f"></param>
		/// <param name="position"></param>
		public static void SetPosition(this EntityRef entity, Frame f, FPVector3 position)
		{
			f.Unsafe.GetPointer<Transform3D>(entity)->Position = position;
		}

		/// <summary>
		/// Calculates and returns an augmented shot angle based on approximation of normal distribution
		/// </summary>
		/// <remarks>
		/// Accuracy modifier is found by approximate normal distribution random,
		/// and then creating a rotation vector that is passed onto the projectile; only works for single shot weapons
		/// </remarks>
		public static FP GetSingleShotAngleAccuracyModifier(Frame f, FP targetAttackAngle)
		{
			var rngNumber = f.RNG->NextInclusive(0, 100);
			var angleStep = targetAttackAngle / Constants.APPRX_NORMAL_DISTRIBUTION.Length;

			for (var i = 0; i < Constants.APPRX_NORMAL_DISTRIBUTION.Length; i++)
			{
				if (rngNumber <= Constants.APPRX_NORMAL_DISTRIBUTION[i])
				{
					return f.RNG->Next(angleStep * i, angleStep * (i + 1)) - (targetAttackAngle / FP._2);
				}
			}

			return FP._0;
		}

		/// <summary>
		/// Returns the aiming directionof the player, and the looking direction if you are not aiming
		/// </summary>
		public static FPVector2 GetAimDirection(FPVector2 attackDirection, in FPQuaternion rotation)
		{
			return attackDirection == FPVector2.Zero ? (rotation * FPVector3.Forward).XZ : attackDirection;
		}

		/// <summary>
		/// Used to sort spawners based on relevancy to the type of player that is spawning. If it's a bot, it will first provide spawners specifically for bots, and so on.
		/// </summary>
		private static Comparison<EntityComponentPointerPair<PlayerSpawner>> PlayerSpawnerPlayerTypeComparison(Frame f, FPVector3 positionToCompare)
		{
			return (pair, pointerPair) =>
			{
				// If its the same spawner type, BotOfType still needs to also compare the Behaviour Type
				if (pair.Component->SpawnerType == pointerPair.Component->SpawnerType &&
					(pair.Component->SpawnerType != SpawnerType.BotOfType || pair.Component->BehaviourType == pointerPair.Component->BehaviourType))
				{
					var pos1 = f.Get<Transform3D>(pair.Entity).Position;
					var pos2 = f.Get<Transform3D>(pointerPair.Entity).Position;

					return FPVector3.DistanceSquared(pos1, positionToCompare) <
						FPVector3.DistanceSquared(pos2, positionToCompare)
							? -1
							: 1;
				}

				return 1;
			};
		}
	}
}