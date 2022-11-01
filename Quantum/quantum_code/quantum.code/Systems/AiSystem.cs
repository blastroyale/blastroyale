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
			var data = &filter.Agent->Data;

			HFSMManager.Update(f, f.DeltaTime, data, filter.Entity);
		}

		public void GameEnded(Frame f)
		{
			foreach (var bb in f.Unsafe.GetComponentBlockIterator<AIBlackboardComponent>())
			{
				bb.Component->Set(f, Constants.IsAimPressedKey, false);
			}
		}
	}
}