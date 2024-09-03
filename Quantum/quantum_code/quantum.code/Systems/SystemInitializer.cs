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
			f.Context.MapConfig = f.MapConfigs.GetConfig(f.RuntimeConfig.MatchConfigs.MapId);
			f.Context.GameModeConfig = f.GameModeConfigs.GetConfig(f.RuntimeConfig.MatchConfigs.GameModeID);
			f.Context.Mutators = f.RuntimeConfig.MatchConfigs.Mutators;
			f.Context.TargetAllLayerMask = -1;
			f.Context.TargetPlayersMask = f.Layers.GetLayerMask(PhysicsLayers.PLAYERS);
			f.Context.TargetMapOnlyLayerMask = f.Layers.GetLayerMask(PhysicsLayers.OBSTACLES);
			f.Context.TargetMapAndPlayersMask = f.Layers.GetLayerMask(PhysicsLayers.PLAYERS_HITBOX,
				PhysicsLayers.PLAYER_TRIGGERS, PhysicsLayers.OBSTACLES);
			f.Context.TargetPlayerLineOfSightLayerMask = f.Layers.GetLayerMask(PhysicsLayers.OBSTACLES);
			f.Context.TargetPlayerTriggersLayerIndex = f.Layers.GetLayerIndex(PhysicsLayers.PLAYER_TRIGGERS);
			f.Context.MapShrinkingCircleConfigs = f.ShrinkingCircleConfigs.GetConfigs(f.RuntimeConfig.MatchConfigs.MapId);

			foreach (var systemName in f.Context.GameModeConfig.Systems)
			{
				var systemType = Type.GetType(systemName);
				if (systemType == null)
				{
					Log.Error($"System {systemName} not found to be initialized by game mode");
					continue;
				}
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
