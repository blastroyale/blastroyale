using System;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system is responsible for handling all the stages of an AirDrop.
	/// </summary>
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

			var initialPos = (circle.CurrentCircleCenter - circle.TargetCircleCenter).Normalized *
			                 circle.CurrentRadius * f.GameConfig.AirdropPositionOffsetMultiplier;
			var radius = circle.CurrentRadius * f.GameConfig.AirdropRandomAreaMultiplier;
			QuantumHelpers.TryFindPosOnNavMesh(f, initialPos.XOY, radius, out var dropPosition);

			component->Position = dropPosition;
			component->StartTime = f.Time;

			var transform = f.Unsafe.GetPointer<Transform3D>(entity);
			transform->Position = dropPosition + FPVector3.Up * f.GameConfig.AirdropHeight;
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
					var transform = f.Unsafe.GetPointer<Transform3D>(filter.Entity);

					var modifier = FP._1 - (f.Time - drop->StartTime) / (drop->Delay + drop->Duration);
					transform->Position = drop->Position + FPVector3.Up * f.GameConfig.AirdropHeight * modifier;

					if (f.Time >= drop->StartTime + drop->Delay + drop->Duration)
					{
						drop->Stage = AirDropStage.Dropped;

						f.Add<Chest>(filter.Entity, out var chest);
						chest->Init(f, filter.Entity, drop->Position, FPQuaternion.Identity,
						            f.ChestConfigs.GetConfig(drop->Chest));

						f.Events.OnAirDropDropped(filter.Entity, f.Get<AirDrop>(filter.Entity));
					}

					break;
				case AirDropStage.Dropped:
					// Do nothing
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}