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
				var radialDir = f.RNG->Next(0, FP.Rad_180 * 2);
				var radius = circle.TargetRadius * (1 + f.GameConfig.AirdropPositionOffsetMultiplier);
				var x = radius * FPMath.Sin(radialDir) + circle.TargetCircleCenter.X;
				var y = radius * FPMath.Cos(radialDir) + circle.TargetCircleCenter.Y;
				FPVector3 pos = new FPVector3(x, 0, y);

				QuantumHelpers.TryFindPosOnNavMesh(f, pos, circle.TargetRadius * f.GameConfig.AirdropRandomAreaMultiplier, out dropPosition);
			}
			
			// Move entity to the drop position at a predetermined height
			var transform = f.Unsafe.GetPointer<Transform3D>(entity);
			transform->Position = dropPosition + FPVector3.Up * f.GameConfig.AirdropHeight;

			var airDrop = new AirDrop
			{
				Delay = f.RNG->NextInclusive(config.AirdropStartTimeRange.Value1, config.AirdropStartTimeRange.Value2),
				Duration = config.AirdropDropDuration,
				Stage = AirDropStage.Waiting,
				Chest = config.AirdropChest,
				Position = dropPosition,
				Direction = FPVector2.Rotate(FPVector2.Up, FP.Pi * f.RNG->Next(FP._0, FP._2)),
				StartTime = f.Time
			};
			f.Add(entity, airDrop);

			return entity;
		}
	}
}