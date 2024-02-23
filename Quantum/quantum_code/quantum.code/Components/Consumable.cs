using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Consumable
	{
		/// <summary>
		/// Initializes this Consumable with all the necessary data
		/// </summary>
		internal void Init(Frame f, EntityRef e, FPVector3 position, FPQuaternion rotation,
						   ref QuantumConsumableConfig config, EntityRef spawner, FPVector3 originPos)
		{
			var collectable = new Collectable
			{
				GameId = config.Id, PickupRadius = config.CollectableConsumablePickupRadius,
				AllowedToPickupTime = f.Time + Constants.CONSUMABLE_POPOUT_DURATION
			};
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			ConsumableType = config.ConsumableType;
			Amount = config.Amount.Get(f);
			CollectTime = config.ConsumableCollectTime.Get(f);

			collectable.Spawner = spawner;
			collectable.OriginPosition = originPos;

			transform->Position = position;
			transform->Rotation = rotation;

			f.Add(e, collectable);

			var collider = f.Unsafe.GetPointer<PhysicsCollider3D>(e);
			collider->Shape.Sphere.Radius = config.CollectableConsumablePickupRadius;
		}

		/// <summary>
		/// Collects this given <paramref name="entity"/> by the given <paramref name="player"/>
		/// </summary>
		internal void Collect(Frame f, EntityRef entity, EntityRef playerEntity, PlayerRef player)
		{
			var stats = f.Unsafe.GetPointer<Stats>(playerEntity);
			var isTeamsMode = f.Context.GameModeConfig.Teams;
			var team = f.Get<Targetable>(playerEntity).Team;
			var collectable = f.Unsafe.GetPointer<Collectable>(entity);
			
			// TODO: switch to signal handlers on specific systems
			switch (ConsumableType)
			{
				case ConsumableType.Health:
					var spell = new Spell {PowerAmount = (uint) Amount.AsInt};
					stats->GainHealth(f, playerEntity, &spell);
					break;
				case ConsumableType.Rage:
					StatusModifiers.AddStatusModifierToEntity(f, playerEntity, StatusModifierType.Rage, Amount.AsInt);
					break;
				case ConsumableType.Ammo:
					stats->GainAmmoPercent(f, playerEntity, Amount);
					break;
				case ConsumableType.Shield:
					stats->GainShield(f, playerEntity, Amount.AsInt);
					break;
				case ConsumableType.ShieldCapacity:
					stats->GainShieldCapacity(f, playerEntity, Amount.AsInt);
					break;
				case ConsumableType.Special:
					f.Unsafe.GetPointer<PlayerInventory>(playerEntity)->TryAddSpecial(f, playerEntity, player,
						new Special(f, collectable->GameId));
					break;
				case ConsumableType.GameItem:
					// Handled in GameItemCollectableSystem
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (isTeamsMode && f.Context.TryGetMutatorByType(MutatorType.ConsumablesSharing, out _))
			{
				ShareCollectWithTeammates(f, playerEntity, team);
			}

			f.Signals.OnConsumableCollected(player, playerEntity, this, *collectable);
			f.Events.OnConsumableCollected(entity, player, playerEntity);
		}

		private void ShareCollectWithTeammates(Frame f, EntityRef playerEntity, int team)
		{
			// Rage and ShieldsCapacity are not shared with teammates
			if (ConsumableType == ConsumableType.Rage || ConsumableType == ConsumableType.ShieldCapacity)
			{
				return;
			}

			foreach (var teammateCandidate in f.Unsafe.GetComponentBlockIterator<Targetable>())
			{
				if (teammateCandidate.Entity != playerEntity &&
					!QuantumHelpers.IsDestroyed(f, teammateCandidate.Entity) &&
					teammateCandidate.Component->Team == team)
				{
					var stats = f.Unsafe.GetPointer<Stats>(teammateCandidate.Entity);
					var playerChar = f.Unsafe.GetPointer<PlayerCharacter>(teammateCandidate.Entity);
					switch (ConsumableType)
					{
						case ConsumableType.Health:
							var spell = new Spell {PowerAmount = (uint) Amount.AsInt};
							stats->GainHealth(f, teammateCandidate.Entity, &spell);
							break;
						case ConsumableType.Ammo:
							stats->GainAmmoPercent(f, teammateCandidate.Entity, Amount);
							break;
						case ConsumableType.Shield:
							stats->GainShield(f, teammateCandidate.Entity, Amount.AsInt);
							break;
					}
				}
			}
		}
	}
}