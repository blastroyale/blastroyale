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
			circle->CurrentRadius = f.GameConfig.BattleRoyaleMapRadius;
			
			SetShrinkingCircleData(f, circle, f.BattleRoyaleConfigs.QuantumConfigs[0]);
		}

		/// <inheritdoc />
		public override void Update(Frame f)
		{
			var circle = ProcessShrinkingCircle(f);
			
			if (f.Time < circle.ShrinkingStartTime)
			{
				return;
			}
			
			var lerp = FPMath.Max(0, (f.Time - circle.ShrinkingStartTime) / circle.ShrinkingDurationTime);
			var radius = FPMath.Lerp(circle.CurrentRadius, circle.TargetRadius, lerp);
			var center = FPVector2.Lerp(circle.CurrentCircleCenter, circle.TargetCircleCenter, lerp);

			radius = radius * radius;
			
			foreach (var pair in f.GetComponentIterator<AlivePlayerCharacter>())
			{
				var position = f.Get<Transform3D>(pair.Entity).Position;
				var distance = (position.XZ - center).SqrMagnitude;

				if (distance > radius)
				{
					f.Unsafe.GetPointer<PlayerCharacter>(pair.Entity)->Dead(f, pair.Entity, PlayerRef.None, EntityRef.None);
				}
			}
		}

		private ShrinkingCircle ProcessShrinkingCircle(Frame f)
		{
			var circle = f.Unsafe.GetPointerSingleton<ShrinkingCircle>();

			if (f.Time < circle->ShrinkingStartTime + circle->ShrinkingDurationTime)
			{
				return *circle;
			}
			
			var configs = f.BattleRoyaleConfigs.QuantumConfigs;

			if (circle->Step >= configs.Count)
			{
				circle->ShrinkingStartTime = FP.MaxValue;
				circle->ShrinkingDurationTime = FP.MaxValue;
					
				return *circle;
			}
			
			circle->ShrinkingStartTime += circle->ShrinkingDurationTime;
			circle->CurrentRadius = circle->TargetRadius;

			SetShrinkingCircleData(f, circle, configs[circle->Step]);
			
			return *circle;
		}

		private void SetShrinkingCircleData(Frame f, ShrinkingCircle* circle, QuantumBattleRoyaleConfig config)
		{
			var gameConfig = f.GameConfig;
			var borderK = gameConfig.ShrinkingSizeK * gameConfig.ShrinkingBorderK;

			circle->Step = config.Step;
			circle->ShrinkingStartTime += config.DelayTime + config.WarningTime;
			circle->ShrinkingDurationTime = config.ShringkingTime;
			circle->CurrentCircleCenter = circle->TargetCircleCenter;
			circle->TargetRadius = circle->CurrentRadius * gameConfig.ShrinkingSizeK;
			circle->TargetCircleCenter += new FPVector2(f.RNG->NextInclusive(-borderK, borderK), 
			                                            f.RNG->NextInclusive(-borderK, borderK)) * circle->TargetRadius;
			
			f.Events.OnNewShrinkingCircle(*circle);
		}
	}
}