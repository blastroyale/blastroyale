using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="ShrinkingCircle"/>
	/// </summary>
	public unsafe class ShrinkingCircleSystem : SystemMainThread, ISignalOnComponentAdded<ShrinkingCircle>
	{
		/// <inheritdoc />
		public override bool StartEnabled => false;

		/// <inheritdoc />
		public void OnAdded(Frame f, EntityRef entity, ShrinkingCircle* circle)
		{
			circle->CurrentRadius = f.Map.WorldSize / FP._2;
			
			SetShrinkingCircleData(f, circle, f.ShrinkingCircleConfigs.QuantumConfigs[0]);
		}

		/// <inheritdoc />
		public override void Update(Frame f)
		{
			var circle = ProcessShrinkingCircle(f);
			var lerp = FPMath.Max(0, (f.Time - circle->ShrinkingStartTime) / circle->ShrinkingDurationTime);
			var radius = FPMath.Lerp(circle->CurrentRadius, circle->TargetRadius, lerp);
			var center = FPVector2.Lerp(circle->CurrentCircleCenter, circle->TargetCircleCenter, lerp);
			
			radius *= radius;
			
			foreach (var pair in f.GetComponentIterator<AlivePlayerCharacter>())
			{
				var transform = f.Get<Transform3D>(pair.Entity);
				var position = transform.Position;
				var distance = (position.XZ - center).SqrMagnitude;

				if (distance > radius)
				{
					var currentHealth = f.Get<Stats>(pair.Entity).CurrentHealth;
					
					f.Signals.HealthIsZero(pair.Entity, EntityRef.None);
					f.Events.OnHealthIsZero(pair.Entity, EntityRef.None, currentHealth);
				}
			}
		}

		private ShrinkingCircle* ProcessShrinkingCircle(Frame f)
		{
			var circle = f.Unsafe.GetPointerSingleton<ShrinkingCircle>();

			if (f.Time < circle->ShrinkingStartTime + circle->ShrinkingDurationTime)
			{
				return circle;
			}
			
			var configs = f.ShrinkingCircleConfigs.QuantumConfigs;

			if (circle->Step >= configs.Count)
			{
				circle->ShrinkingStartTime = FP.MaxValue;
				circle->ShrinkingDurationTime = FP.MaxValue;
					
				return circle;
			}
			
			circle->ShrinkingStartTime += circle->ShrinkingDurationTime;
			circle->CurrentRadius = circle->TargetRadius;

			SetShrinkingCircleData(f, circle, configs[circle->Step]);
			
			return circle;
		}

		private void SetShrinkingCircleData(Frame f, ShrinkingCircle* circle, QuantumShrinkingCircleConfig config)
		{
			var gameConfig = f.GameConfig;
			var borderK = gameConfig.ShrinkingSizeK * gameConfig.ShrinkingBorderK;

			circle->Step = config.Step;
			circle->ShrinkingStartTime += config.DelayTime + config.WarningTime;
			circle->ShrinkingDurationTime = config.ShrinkingTime;
			circle->CurrentCircleCenter = circle->TargetCircleCenter;
			circle->TargetRadius = circle->CurrentRadius * gameConfig.ShrinkingSizeK;
			circle->TargetCircleCenter += new FPVector2(f.RNG->NextInclusive(-borderK, borderK), 
			                                            f.RNG->NextInclusive(-borderK, borderK)) * circle->CurrentRadius;
			
			f.Events.OnNewShrinkingCircle(*circle);
		}
	}
}