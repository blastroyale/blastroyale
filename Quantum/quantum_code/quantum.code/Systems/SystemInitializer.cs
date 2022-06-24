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
			f.Global->Queries = f.AllocateList<EntityPair>(128);
			f.Context.MapConfig = f.MapConfigs.GetConfig(f.RuntimeConfig.MapId);
			f.Context.TargetAllLayerMask = f.Layers.GetLayerMask("Default", "Playable Target", "Non Playable Target",
			                                                     "Prop", "World", "Environment No Silhouette");
			
			f.GetOrAddSingleton<GameContainer>();

			if (f.Context.MapConfig.GameMode == GameMode.BattleRoyale && !f.Context.MapConfig.IsTestMap && 
			    !f.SystemIsEnabledSelf<ShrinkingCircleSystem>())
			{
				f.SystemEnable<ShrinkingCircleSystem>();
				f.GetOrAddSingleton<ShrinkingCircle>();
			}
		}
	}
}