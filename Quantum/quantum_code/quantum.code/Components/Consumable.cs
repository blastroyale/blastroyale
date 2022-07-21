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
			Amount = config.Amount;
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
			var consumable = f.Get<Consumable>(entity);

			switch (ConsumableType)
			{
				case ConsumableType.Health:
					f.Unsafe.GetPointer<Stats>(playerEntity)->GainHealth(f, playerEntity, entity, (uint) consumable.Amount.AsInt);
					break;
				case ConsumableType.Rage:
					StatusModifiers.AddStatusModifierToEntity(f, playerEntity, StatusModifierType.Rage, consumable.Amount.AsInt);
					break;
				case ConsumableType.Ammo:
					f.Unsafe.GetPointer<PlayerCharacter>(playerEntity)->GainAmmo(f, playerEntity, consumable.Amount);
					break;
				case ConsumableType.Shield:
					f.Unsafe.GetPointer<Stats>(playerEntity)->GainShields(f, playerEntity, entity, consumable.Amount.AsInt);
					break;
				case ConsumableType.ShieldCapacity:
					f.Unsafe.GetPointer<Stats>(playerEntity)->GainShieldCapacity(f, playerEntity, entity, consumable.Amount.AsInt);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
