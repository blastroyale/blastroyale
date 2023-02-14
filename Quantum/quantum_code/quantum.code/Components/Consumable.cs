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
		internal void Init(Frame f, EntityRef e, FPVector3 position, FPQuaternion rotation, QuantumConsumableConfig config)
		{
			var collectable = new Collectable {GameId = config.Id};
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			
			ConsumableType = config.ConsumableType;
			Amount = config.Amount.Get(f);
			CollectTime = config.ConsumableCollectTime.Get(f);
			
			transform->Position = position;
			transform->Rotation = rotation;
			
			f.Add(e, collectable);
		}

		/// <summary>
		/// Collects this given <paramref name="entity"/> by the given <paramref name="player"/>
		/// </summary>
		internal void Collect(Frame f, EntityRef entity, EntityRef playerEntity, PlayerRef player)
		{
			var stats = f.Unsafe.GetPointer<Stats>(playerEntity);
			var isTeamsMode = f.Context.GameModeConfig.Teams;
			var team = f.Get<Targetable>(playerEntity).Team;

			switch (ConsumableType)
			{
				case ConsumableType.Health:
					stats->GainHealth(f, playerEntity, new Spell { PowerAmount = (uint) Amount.AsInt});
					
					if (isTeamsMode)
					{
						foreach (var teammateCandidate in f.Unsafe.GetComponentBlockIterator<Targetable>())
						{
							if (!QuantumHelpers.IsDestroyed(f, teammateCandidate.Entity) && teammateCandidate.Component->Team == team)
							{
								stats->GainHealth(f, teammateCandidate.Entity, new Spell { PowerAmount = (uint) Amount.AsInt});
							}
						}
					}
					
					break;
				case ConsumableType.Rage:
					StatusModifiers.AddStatusModifierToEntity(f, playerEntity, StatusModifierType.Rage, Amount.AsInt);
					break;
				case ConsumableType.Ammo:
					f.Unsafe.GetPointer<Stats>(playerEntity)->GainAmmoPercent(f, playerEntity, Amount);
					
					if (isTeamsMode)
					{
						foreach (var teammateCandidate in f.Unsafe.GetComponentBlockIterator<Targetable>())
						{
							if (!QuantumHelpers.IsDestroyed(f, teammateCandidate.Entity) && teammateCandidate.Component->Team == team)
							{
								f.Unsafe.GetPointer<Stats>(teammateCandidate.Entity)->GainAmmoPercent(f, teammateCandidate.Entity, Amount);
							}
						}
					}
					
					break;
				case ConsumableType.Shield:
					stats->GainShield(f, playerEntity, Amount.AsInt);
					
					if (isTeamsMode)
					{
						foreach (var teammateCandidate in f.Unsafe.GetComponentBlockIterator<Targetable>())
						{
							if (!QuantumHelpers.IsDestroyed(f, teammateCandidate.Entity) && teammateCandidate.Component->Team == team)
							{
								stats->GainShield(f, teammateCandidate.Entity, Amount.AsInt);
							}
						}
					}
					
					break;
				case ConsumableType.ShieldCapacity:
					stats->GainShieldCapacity(f, playerEntity, Amount.AsInt);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			f.Events.OnConsumableCollected(entity, player, playerEntity, this);
		}
	}
}
