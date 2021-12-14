using System;
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
			Amount = config.PowerAmount;
			
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
					f.Unsafe.GetPointer<Stats>(playerEntity)->GainHealth(f, playerEntity, entity, (int) consumable.Amount);
					break;
				case ConsumableType.Rage:
					StatusModifiers.AddStatusModifierToEntity(f, playerEntity, StatusModifierType.Rage, (int) consumable.Amount);
					break;
				case ConsumableType.Ammo:
					f.Unsafe.GetPointer<Weapon>(playerEntity)->GainAmmo(consumable.Amount);
					break;
				case ConsumableType.InterimArmour:
					f.Unsafe.GetPointer<Stats>(playerEntity)->GainInterimArmour(f, playerEntity, entity, (int) consumable.Amount);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}