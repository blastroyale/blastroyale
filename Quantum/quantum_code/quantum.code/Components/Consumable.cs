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

			switch (ConsumableType)
			{
				case ConsumableType.Health:
					stats->GainHealth(f, playerEntity, new Spell { PowerAmount = (uint) Amount.AsInt});
					break;
				case ConsumableType.Rage:
					StatusModifiers.AddStatusModifierToEntity(f, playerEntity, StatusModifierType.Rage, Amount.AsInt);
					break;
				case ConsumableType.Ammo:
					f.Unsafe.GetPointer<PlayerCharacter>(playerEntity)->GainAmmo(f, playerEntity, Amount);
					break;
				case ConsumableType.Shield:
					stats->GainShield(f, playerEntity, Amount.AsInt);
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
