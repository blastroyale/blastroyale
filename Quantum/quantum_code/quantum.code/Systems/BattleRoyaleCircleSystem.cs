using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="BattleRoyaleCircle"/>
	/// </summary>
	public unsafe class BattleRoyaleCircleSystem : SystemMainThread
	{
		/// <inheritdoc />
		public override bool StartEnabled => false;

		/// <inheritdoc />
		public override void Update(Frame f)
		{
			var circle = f.GetSingleton<BattleRoyaleCircle>();
			// TODO: update shrinkning circle;
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

		private BattleRoyaleCircle ProcessShrinkingCircle(Frame f)
		{
			var circle = f.Unsafe.GetPointerSingleton<BattleRoyaleCircle>();

			if (f.Time > circle->ShrinkingStartTime + circle->ShrinkingDurationTime)
			{
				// TODO: times
			}
			
			return *circle;
		}
	}
}