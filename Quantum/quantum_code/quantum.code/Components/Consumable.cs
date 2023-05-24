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
		internal void Init(Frame f, EntityRef e, FPVector3 position, FPQuaternion rotation, ref QuantumConsumableConfig config, EntityRef spawner)
		{
			var collectable = new Collectable {GameId = config.Id, PickupRadius = config.CollectableConsumablePickupRadius};
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			
			ConsumableType = config.ConsumableType;
			Amount = config.Amount.Get(f);
			CollectTime = config.ConsumableCollectTime.Get(f);

			collectable.Spawner = spawner;
			
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
			var playerChar = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			var stats = f.Unsafe.GetPointer<Stats>(playerEntity);
			var isTeamsMode = f.Context.GameModeConfig.Teams;
			var team = f.Get<Targetable>(playerEntity).Team;

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
					f.Unsafe.GetPointer<Stats>(playerEntity)->GainAmmoPercent(f, playerEntity, Amount);
					break;
				case ConsumableType.Shield:
					stats->GainShield(f, playerEntity, Amount.AsInt);
					break;
				case ConsumableType.ShieldCapacity:
					stats->GainShieldCapacity(f, playerEntity, Amount.AsInt);
					break;
				case ConsumableType.Energy:
					playerChar->GainEnergy(f, playerEntity, Amount.AsInt);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			
			if (isTeamsMode)
			{
				ShareCollectWithTeammates(f, playerEntity, team);
			}
			
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
							f.Unsafe.GetPointer<Stats>(teammateCandidate.Entity)->GainAmmoPercent(f, teammateCandidate.Entity, Amount);
							break;
						case ConsumableType.Shield:
							stats->GainShield(f, teammateCandidate.Entity, Amount.AsInt);
							break;
						case ConsumableType.Energy:
							playerChar->GainEnergy(f, playerEntity, Amount.AsInt);
							break;
					}
				}
			}
		}
	}
}
