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
			
			f.Global->Queries = f.AllocateList<EntityPair>(128);
			
			gameContainer->TargetProgress = (uint) f.RuntimeConfig.GameEndTarget;
			gameContainer->GameMode = f.RuntimeConfig.GameMode;

			if (gameContainer->GameMode == GameMode.BattleRoyale)
			{
				f.SystemEnable<BattleRoyaleCircleSystem>();
				f.GetOrAddSingleton<BattleRoyaleCircle>();
			}
		}
	}
}