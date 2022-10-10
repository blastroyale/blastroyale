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
			var circle = f.GetSingleton<ShrinkingCircle>();
			
			var dropPosition = positionOverride;
			if (dropPosition == FPVector3.Zero && !GetDropPosition(f, circle, out dropPosition))
			{
				return EntityRef.None;
			}
			
			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.AirDropPrototype.Id));
			
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

		private static bool GetDropPosition(Frame f, ShrinkingCircle circle, out FPVector3 dropPosition)
		{
			var radialDir = f.RNG->Next(0, FP.Rad_180 * 2);
			var radius = FPMath.Lerp(circle.TargetRadius, circle.CurrentRadius, f.GameConfig.AirdropPositionOffsetMultiplier);
			var areaCenter = FPVector2.Lerp(circle.TargetCircleCenter, circle.CurrentCircleCenter,
			                                f.GameConfig.AirdropPositionOffsetMultiplier);
			var areaCenterV3 = new FPVector3(areaCenter.X, 0, areaCenter.Y);
			var x = radius * FPMath.Sin(radialDir) + areaCenter.X;
			var y = radius * FPMath.Cos(radialDir) + areaCenter.Y;
			var pos = new FPVector3(x, 0, y);
			var squareRadiusArea = radius * radius;
			
			var found = false;
			var closestPoint = FPVector3.Zero;
			var shortestDist = FP.MaxValue;

			foreach(var spawner in f.GetComponentIterator<AirDropSpawner>())
			{
				var spawnerPos = spawner.Entity.GetPosition(f);
				var distanceToSpawner = FPVector3.DistanceSquared(pos, spawnerPos);

				var insideArea = FPVector3.DistanceSquared(spawnerPos, areaCenterV3) < squareRadiusArea;

				if (distanceToSpawner < shortestDist & insideArea)
				{
					shortestDist = distanceToSpawner;
					closestPoint = spawnerPos;

					found = true;
				}	
			}

			dropPosition = closestPoint;

			return found;
		}
	}
}