using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This class contains various helper functions to use inside Quantum
	/// </summary>
	public static unsafe class QuantumHelpers
	{
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
		public static void LookAt2d(Frame f, EntityRef e, EntityRef target)
		{
			LookAt2d(f, e, f.Get<Transform3D>(target).Position);
		}

		/// <inheritdoc cref="LookAt2d(Quantum.Frame,Quantum.EntityRef,Quantum.EntityRef)"/>
		public static void LookAt2d(Frame f, EntityRef e, FPVector3 target)
		{
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			var direction = target - transform->Position;

			LookAt2d(transform, direction.XZ);
		}

		/// <summary>
		/// Makes the given entity <paramref name="e"/> rotate in the XZ axis in the given <paramref name="direction"/>
		/// </summary>
		public static void LookAt2d(Frame f, EntityRef e, FPVector2 direction)
		{
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			LookAt2d(transform, direction);
		}

		/// <summary>
		/// Makes the given entity <paramref name="transform"/> rotate in the XZ axis in the given <paramref name="direction"/>
		/// </summary>
		public static void LookAt2d(Transform3D* transform, FPVector2 direction)
		{
			var angle = FPMath.Atan2(direction.X, direction.Y);
			
			transform->Rotation = FPQuaternion.AngleAxis(angle * FP.Rad2Deg, FPVector3.Up);
		}
		
		/// <summary>
		/// Determines if <paramref name="target"/> entity is between <paramref name="minRange"/> and <paramref name="maxRange"/> of another entity
		/// </summary>
		public static bool IsEntityInRange(Frame f, EntityRef e, EntityRef target, FP minRange, FP maxRange)
		{
			var position = f.Get<Transform3D>(target).Position;
			var sqrDistance = (f.Get<Transform3D>(e).Position - position).SqrMagnitude;
			
			return sqrDistance >= (minRange * minRange) && sqrDistance <= (maxRange * maxRange);
		}
		
		/// <summary>
		/// Determines if <paramref name="e"/> entity is valid, exists, not marked on destroy and targetable
		/// </summary>
		public static bool IsAttackable(Frame f, EntityRef e, int attackerTeam)
		{
			var neutral = (int)TeamType.Neutral;
			
			if (f.GetSingleton<GameContainer>().IsGameOver)
			{
				return false;
			}
			
			return !IsDestroyed(f, e) && f.TryGet<Targetable>(e, out var targetable) &&
			       (targetable.Team != attackerTeam || targetable.Team == neutral || attackerTeam == neutral);
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
		public static bool ProcessAreaHit(Frame f, FP radius, Spell spell, uint maxHitCount = uint.MaxValue,
		                                  Action<Frame, Spell> onHitCallback = null)
		{
			if (f.GetSingleton<GameContainer>().IsGameOver)
			{
				return false;
			}
			
			var hitCount = 0;
			var shape = Shape3D.CreateSphere(radius);
			var hits = f.Physics3D.OverlapShape(spell.OriginalHitPosition, FPQuaternion.Identity, shape, 
			                                    f.Context.TargetAllLayerMask, QueryOptions.HitDynamics | QueryOptions.HitKinematics);
			
			hits.SortCastDistance();

			for (var j = 0; j < hits.Count; j++)
			{
				var hitSpell = Spell.CreateInstant(f, hits[j].Entity, spell.Attacker, spell.SpellSource,
				                                   spell.PowerAmount, spell.KnockbackAmount, hits[j].Point, spell.TeamSource);

				if (hitSpell.Victim == spell.Attacker)
				{
					hitSpell.TeamSource = 0;
					//TODO: this self damage modifier should take into account equipment modifiers once we have it, for now it's just a constant
					hitSpell.PowerAmount = (uint)(spell.PowerAmount * Constants.SELF_DAMAGE_MODIFIER); 
				}

				if (!ProcessHit(f, hitSpell))
				{
					continue;
				}

				hitCount++;
					
				onHitCallback?.Invoke(f, hitSpell);

				if (hitCount >= maxHitCount)
				{
					break;
				}
			}

			return hitCount > 0;
		}

		/// <summary>
		/// Process a hit source from the given <paramref name="spell"/> to be processed.
		/// Returns true if the hit was successful and false otherwise
		/// </summary>
		public static bool ProcessHit(Frame f, Spell spell)
		{
			if (!IsAttackable(f, spell.Victim, spell.TeamSource))
			{
				return false;
			}

			if (spell.KnockbackAmount > 0 &&
			    f.Unsafe.TryGetPointer<CharacterController3D>(spell.Victim, out var kcc) &&
			    f.TryGet<Transform3D>(spell.Victim, out var victimTransform))
			{
				var kick = (victimTransform.Position - spell.OriginalHitPosition).Normalized *
				           spell.KnockbackAmount;
				kick.Y = FP._0;
				kcc->Velocity += kick;
			}

			f.Add(f.Create(), spell);

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
		/// Set's the navmesh agent of the given <paramref name="e"/> entity's target position to as closest as possible
		/// to the given <paramref name="destination"/>.
		/// If the given <paramref name="destination"/> is invalid then the entity's navmesh agent will not move
		/// </summary>
		public static bool SetClosestTarget(Frame f, EntityRef e, FPVector3 destination)
		{
			var agent = f.Unsafe.GetPointer<NavMeshPathfinder>(e);
			var config = f.FindAsset<NavMeshAgentConfig>(agent->ConfigId);
			var navMesh = f.NavMesh;
			
			if (navMesh.FindRandomPointOnNavmesh(destination, config.AutomaticTargetCorrectionRadius, f.RNG, 
			                                     agent->RegionMask, out var closestPosition))
			{
				agent->SetTarget(f, closestPosition, navMesh);
				
				return true;
			}
			
			return false;
		}
		
		/// <summary>
		/// Set's the navmesh agent of the given <paramref name="e"/> entity's target position to as close as possible
		/// to the random position within <paramref name="radius"/> from <paramref name="startPosition"/>.
		/// If the position is not found then the entity's navmesh agent will not move
		/// </summary>
		public static bool SetClosestTarget(Frame f, EntityRef e, FPVector3 startPosition, FP radius)
		{
			var agent = f.Unsafe.GetPointer<NavMeshPathfinder>(e);
			var navMesh = f.NavMesh;
			
			if (navMesh.FindRandomPointOnNavmesh(startPosition, radius, f.RNG, 
			                                     agent->RegionMask, out var closestPosition))
			{
				agent->SetTarget(f, closestPosition, navMesh);
				
				return true;
			}
			
			return false;
		}

		/// <summary>
		/// Requests a valid <see cref="Transform3D"/> from one of the <see cref="PlayerSpawner"/> points present in the world.
		/// A spawn point is valid if no one is in it.
		/// If all spawn points are filled with players, then the last point on the list will be picked.
		/// It also activates the return spawn point
		/// It also changes the bot's Behaviour type if the spawn point has ForceStatic
		/// </summary>
		public static EntityComponentPair<Transform3D> GetPlayerSpawnTransform(Frame f, EntityRef playerEntity )
		{
			var spawners = new List<EntityComponentPointerPair<PlayerSpawner>>();

			foreach (var pair in f.Unsafe.GetComponentBlockIterator<PlayerSpawner>())
			{
				if (f.Time < pair.Component->ActivationTime)
				{
					continue;
				}
				
				spawners.Add(pair);
			}

			BotCharacter* botCharacter = null;
			var isBot = f.Has<BotCharacter>(playerEntity);
			if (isBot)
			{
				botCharacter = f.Unsafe.GetPointer<BotCharacter>(playerEntity);
			}


			spawners.Sort((pair, pointerPair) =>
			{
				// If its the same spawner type, BotOfType still needs to also compare the Behaviour Type
				if (pair.Component->SpawnerType == pointerPair.Component->SpawnerType && 
					(pair.Component->SpawnerType!= SpawnerType.BotOfType || pair.Component->BehaviourType == pointerPair.Component->BehaviourType))
				{
					// Making it random for the similar ones, will make it so they are randomly sorted between them, making the next one random
					return f.RNG->Next(-1, 2);
				}
				
				if (!isBot)
				{
					if (pair.Component->SpawnerType == SpawnerType.Player)
					{
						return -1;
					}
				}
				else
				{
					if (pointerPair.Component->SpawnerType == SpawnerType.Player)
						return -1;
					if (pair.Component->SpawnerType == SpawnerType.BotOfType)
					{
						return pair.Component->BehaviourType == botCharacter->BehaviourType ? -1 : 1;
					}
					if (pointerPair.Component->SpawnerType == SpawnerType.BotOfType)
					{
						return pointerPair.Component->BehaviourType == botCharacter->BehaviourType ? 1 : -1;
					}
					if (pair.Component->SpawnerType == SpawnerType.AnyBot)
					{
						return -1;
					}
				}

				return 1;
			});

			if (spawners.Count == 0)
			{
				Log.Error($"There is no {nameof(PlayerSpawner)} active to spawn new a player");
			}

			var entity = spawners[0].Entity;

			if (isBot && spawners[0].Component->ForceStatic)
			{
				botCharacter->BehaviourType = BotBehaviourType.Static;
			}
			
			f.Unsafe.GetPointer<PlayerSpawner>(entity)->ActivationTime = f.Time + Constants.SPAWNER_INACTIVE_TIME;

			return new EntityComponentPair<Transform3D>
			{
				Component = f.Get<Transform3D>(entity), 
				Entity = entity
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
		/// Calculates and returns an augmented shot angle based on approximation of normal distribution
		/// </summary>
		/// <remarks>
		/// Accuracy modifier is found by approximate normal distribution random,
		/// and then creating a rotation vector that is passed onto the projectile; only works for single shot weapons
		/// </remarks>
		public static FP GetSingleShotAngleAccuracyModifier(Frame f, FP targetAttackAngle)
		{
			var rngNumber = f.RNG->NextInclusive(0,100);
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
		/// Calculates a value based on your current movement value
		/// </ summary>
		public static FP GetDynamicAimValue(CharacterController3D* kcc, FP movingValue, FP stationaryValue)
		{
			var cVelocitySqr = kcc->Velocity.SqrMagnitude;
			var maxSpeedSqr = kcc->MaxSpeed * kcc->MaxSpeed;
			if (maxSpeedSqr == 0)
			{
				return 0;
			}
			return FPMath.Lerp(stationaryValue, movingValue, cVelocitySqr / maxSpeedSqr);
		}

		/// <summary>
		/// Returns the aiming directionof the player, and the looking direction if you are not aiming
		/// </summary>
		public static FPVector2 GetAimDirection(FPVector2 attackDirection, FPQuaternion rotation)
		{
			var lookDirection = (rotation * FPVector3.Forward).XZ;
			return attackDirection == FPVector2.Zero ? lookDirection : attackDirection;
		}
	}
}