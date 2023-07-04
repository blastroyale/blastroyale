using System;
using System.Linq;

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
			f.Context.MutatorConfigs = f.RuntimeConfig.Mutators
			                            .Select(mutatorId => f.MutatorConfigs.GetConfig(mutatorId)).ToList();
			f.Context.TargetAllLayerMask = f.Layers.GetLayerMask("Default", "Playable Target", "Non Playable Target", "Non Playable Static Target",
			                                                     "Prop", "World", "Environment No Silhouette", "Environment Object");
			f.Context.TargetPlayersMask = f.Layers.GetLayerMask("Playable Target", "Non Playable Target");
			f.Context.MapShrinkingCircleConfigs = f.ShrinkingCircleConfigs.GetConfigs(f.RuntimeConfig.MapId);
			
			foreach (var systemName in f.Context.GameModeConfig.Systems)
			{
				var systemType = Type.GetType(systemName);
				
				if (!f.SystemIsEnabledSelf(systemType))
				{
					f.SystemEnable(systemType);
				}
			}
		}

		public override void OnEnabled(Frame f)
		{
			f.GetOrAddSingleton<GameContainer>();
			base.OnEnabled(f);
		}
	}
}