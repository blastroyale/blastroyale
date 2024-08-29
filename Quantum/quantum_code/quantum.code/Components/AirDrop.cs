using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct AirDrop
	{
		/// <summary>
		/// Initializes this <see cref="AirDrop"/> with values from <see cref="QuantumShrinkingCircleConfig"/>
		/// </summary>
		public static EntityRef Create(Frame f, in QuantumShrinkingCircleConfig config,
									   FPVector2 positionOverride = new FPVector2())
		{
			var dropPosition = positionOverride;
			if (dropPosition == FPVector2.Zero)
			{
				var circle = f.GetSingleton<ShrinkingCircle>();
				if (!GetDropPosition(f, &circle, out dropPosition))
				{
					return EntityRef.None;
				}
			}

			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.AirDropPrototype.Id));

			// Move entity to the drop position at a predetermined height
			var transform = f.Unsafe.GetPointer<Transform2D>(entity);
			transform->Position = dropPosition + FPVector2.Up * f.GameConfig.AirdropHeight;

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

		private static bool GetDropPosition(Frame f, ShrinkingCircle* circle, out FPVector2 dropPosition)
		{
			var radialDir = f.RNG->Next(0, FP.Rad_180 * 2);
			var radius = FPMath.Lerp(circle->TargetRadius, circle->CurrentRadius, f.GameConfig.AirdropPositionOffsetMultiplier);
			var areaCenter = FPVector2.Lerp(circle->TargetCircleCenter, circle->CurrentCircleCenter,
				f.GameConfig.AirdropPositionOffsetMultiplier);
			var x = radius * FPMath.Sin(radialDir) + areaCenter.X;
			var y = radius * FPMath.Cos(radialDir) + areaCenter.Y;
			var pos = new FPVector2(x, y);
			var squareRadiusArea = radius * radius;

			var found = false;
			var closestPoint = FPVector2.Zero;
			var shortestDist = FP.MaxValue;
			var spawnerEntity = EntityRef.None;

			foreach (var spawner in f.Unsafe.GetComponentBlockIterator<AirDropSpawner>())
			{
				var spawnerPos = spawner.Entity.GetPosition(f);
				var distanceToSpawner = FPVector2.DistanceSquared(pos, spawnerPos);

				var insideArea = FPVector2.DistanceSquared(spawnerPos, areaCenter) < squareRadiusArea;

				if (distanceToSpawner < shortestDist & insideArea)
				{
					shortestDist = distanceToSpawner;
					closestPoint = spawnerPos;

					found = true;
					spawnerEntity = spawner.Entity;
				}
			}

			dropPosition = closestPoint;

			// We remove component to prevent the second use of the same airdrop spawner
			if (found)
			{
				f.Remove<AirDropSpawner>(spawnerEntity);
			}

			return found;
		}
	}
}