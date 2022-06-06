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
			var gameContainer = f.Unsafe.GetOrAddSingletonPointer<GameContainer>();

			f.Context.MapConfig = f.MapConfigs.GetConfig(f.RuntimeConfig.MapId);
			f.Global->Queries = f.AllocateList<EntityPair>(128);
			
			gameContainer->TargetProgress = f.Context.MapConfig.GameEndTarget;

			if (f.Context.MapConfig.GameMode == GameMode.BattleRoyale && !f.Context.MapConfig.IsTestMap)
			{
				f.SystemEnable<ShrinkingCircleSystem>();
				f.GetOrAddSingleton<ShrinkingCircle>();
			}
		}
	}
}