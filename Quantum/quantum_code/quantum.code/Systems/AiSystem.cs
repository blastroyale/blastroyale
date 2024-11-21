using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="HFSMAgent"/> agents in the game
	/// </summary>
	public unsafe class AiSystem : SystemMainThreadFilter<AiSystem.AiFilter>, ISignalGameEnded
	{
		public struct AiFilter
		{
			public EntityRef Entity;
			public HFSMAgent* Agent;
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref AiFilter filter)
		{
			HFSMManager.Update(f, f.DeltaTime, &filter.Agent->Data, filter.Entity);
		}

		public void GameEnded(Frame f, QBoolean _)
		{
			foreach (var bb in f.Unsafe.GetComponentBlockIterator<AIBlackboardComponent>())
			{
				var kcc = f.Unsafe.GetPointer<TopDownController>(bb.Entity);
				bb.Component->Set(f, Constants.IS_AIM_PRESSED_KEY, false);
				bb.Component->Set(f, Constants.AIM_DIRECTION_KEY, FPVector2.Zero);
				kcc->MoveDirection = FPVector2.Zero;
				kcc->AimDirection = FPVector2.Zero;
			}
		}
	}
}