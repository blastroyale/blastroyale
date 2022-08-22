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
			f.Context.GameModeConfig = f.GameModeConfigs.GetConfig(f.RuntimeConfig.GameModeId);
			f.Context.TargetAllLayerMask = f.Layers.GetLayerMask("Default", "Playable Target", "Non Playable Target",
			                                                     "Prop", "World", "Environment No Silhouette");

			f.GetOrAddSingleton<GameContainer>();

			foreach (var system in f.Context.GameModeConfig.Systems)
			{
				if (!f.SystemIsEnabledSelf(system))
				{
					f.SystemEnable(system);
				}

				// TODO: Figure out a better way to do this
				if (system == typeof(ShrinkingCircleSystem))
				{
					f.GetOrAddSingleton<ShrinkingCircle>();
				}
			}
		}
	}
}