namespace Quantum.Systems
{
	/// <summary>
	/// This system initializes all the necessary data that might be needed by all other systems.
	/// ATTENTION: This system runs on every player device independently if is first, middle or last joiner
	/// </summary>
	public unsafe class SystemInitializer : SystemSignalsOnly
	{
		/// <inheritdoc />
		public override void OnInit(Frame f)
		{
			f.Global->Queries = f.AllocateList<EntityPair>(64);
			
			f.Unsafe.GetOrAddSingletonPointer<GameContainer>()->TargetProgress = f.RuntimeConfig.DeathmatchKillCount;
		}
	}
}