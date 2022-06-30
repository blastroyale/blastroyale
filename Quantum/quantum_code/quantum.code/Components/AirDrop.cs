using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct AirDrop
	{
		/// <summary>
		/// Initializes this <see cref="AirDrop"/> with values from <see cref="QuantumShrinkingCircleConfig"/>
		/// </summary>
		public static EntityRef Create(Frame f, QuantumShrinkingCircleConfig config,
		                               FPVector3 positionOverride = new FPVector3())
		{
			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.AirDropPrototype.Id));
			var circle = f.GetSingleton<ShrinkingCircle>();

			// Calculate drop position
			var dropPosition = positionOverride;
			if (dropPosition == FPVector3.Zero)
			{
				var initialPos = (circle.CurrentCircleCenter - circle.TargetCircleCenter).Normalized *
				                 circle.CurrentRadius * f.GameConfig.AirdropPositionOffsetMultiplier;
				var radius = circle.CurrentRadius * f.GameConfig.AirdropRandomAreaMultiplier;
				QuantumHelpers.TryFindPosOnNavMesh(f, initialPos.XOY, radius, out dropPosition);
			}

			// Move entity to the drop position at a predetermined height
			var transform = f.Unsafe.GetPointer<Transform3D>(entity);
			transform->Position = dropPosition + FPVector3.Up * f.GameConfig.AirdropHeight;

			f.Add(entity, new AirDrop
			{
				Delay = f.RNG->NextInclusive(config.AirdropStartTimeRange.Value1, config.AirdropStartTimeRange.Value2),
				Duration = config.AirdropDropDuration,
				Stage = AirDropStage.Waiting,
				Chest = config.AirdropChest,
				Position = dropPosition,
				Direction = FPVector2.Rotate(FPVector2.Up, FP.Pi * f.RNG->Next(FP._0, FP._2)),
				StartTime = f.Time
			});

			return entity;
		}
	}
}