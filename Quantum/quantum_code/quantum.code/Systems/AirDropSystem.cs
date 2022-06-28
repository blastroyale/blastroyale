using System;
using Photon.Deterministic;

namespace Quantum.Systems
{
	public unsafe class AirDropSystem : SystemMainThreadFilter<AirDropSystem.AirDropFilter>,
	                                    ISignalOnComponentAdded<AirDrop>
	{
		public struct AirDropFilter
		{
			public EntityRef Entity;
			public AirDrop* AirDrop;
		}

		public void OnAdded(Frame f, EntityRef entity, AirDrop* component)
		{
			var circle = f.GetSingleton<ShrinkingCircle>();

			var initialPos = ((circle.CurrentCircleCenter - circle.TargetCircleCenter) *
			                  circle.CurrentRadius * f.GameConfig.AirdropPositionOffsetMultiplier);
			var radius = circle.CurrentRadius * f.GameConfig.AirdropRandomAreaMultiplier;

			QuantumHelpers.TryFindPosOnNavMesh(f, initialPos.XOY, radius,
			                                   out var dropPosition);

			component->Position = dropPosition;
			component->StartTime = f.Time;

			f.Unsafe.GetPointer<Chest>(entity)->Init(f, entity, dropPosition, FPQuaternion.Identity,
			                                         f.ChestConfigs.GetConfig(component->Chest));
		}

		public override void Update(Frame f, ref AirDropFilter filter)
		{
			var drop = filter.AirDrop;

			switch (drop->Stage)
			{
				case AirDropStage.Waiting:
					if (f.Time >= drop->StartTime + drop->Delay)
					{
						drop->Stage = AirDropStage.Announcing;
						f.Events.OnAirDropStarted(filter.Entity, f.Get<AirDrop>(filter.Entity));
					}

					break;
				case AirDropStage.Announcing:
					if (f.Time >= drop->StartTime + drop->Delay + drop->Duration)
					{
						drop->Stage = AirDropStage.Dropped;
						// TODO Send signal?
					}

					break;
				case AirDropStage.Dropped:
					// TODO: ?? Do we even need this
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}