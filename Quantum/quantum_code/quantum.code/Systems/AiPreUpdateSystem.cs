namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the pre update behaviour for the <see cref="HFSMAgent"/> agents in the game
	/// </summary>
	public unsafe class AiPreUpdateSystem : SystemMainThreadFilter<AiPreUpdateSystem.AiFilter>
	{
		public struct AiFilter
		{
			public EntityRef Entity;
			public HFSMAgent* Agent;
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref AiFilter filter)
		{
			var currentState = f.FindAsset<HFSMState>(filter.Agent->Data.CurrentState.Id);
			
			currentState.PreUpdateState(f, filter.Entity);
		}
	}
}