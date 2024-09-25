using System.Collections.Generic;
using System.Linq;
using Quantum.Systems;
using Quantum.Systems.Bots;

namespace Quantum
{
	public static class SystemSetup
	{
		public static SystemBase[] CreateSystems(RuntimeConfig gameConfig, SimulationConfig simulationConfig)
		{
			var systems = new List<SystemBase>()
			{
				// Initial pre-defined core systems
				//new Core.CullingSystem3D(),
				new Core.CullingSystem2D(),
				new Core.NavigationSystem(),
				new Core.PlayerConnectedSystem(),

				// Initial Systems
				new SystemInitializer(),
				new AiPreUpdateSystem(),

				// pre-defined core systems
				new Core.PhysicsSystem2D(),
				new Core.EntityPrototypeSystem(),

				// After physics AI system update
				new AiSystem(),

				// Signal only systems - only OnInit & Signal order matters
				new MatchDataSystem(), // Must be the first to guarantee that receives all the events before entities are deleted
				new StatusModifierSystemGroup(),
				new DummyCharacterSystem(),
				new CollectableSystem(),
				new VisibilityAreaSystem(), 
				new GameItemCollectableSystem(),
				new EntityGroupSystem(),
				new LandMineSystem(),
				new CollectableChunkSystem(),
				
				// Update systems - Update & OnInit & Signal order matters
				new CommandsSystem(),
				new ShrinkingCircleSystem(),
				new AirDropSystem(),
				new CollectablePlatformSpawnerSystem(),
				new HazardSystem(),
				new ProjectileSystem(),
				
				new PlayerChargingSystem(),
				new PlayerCharacterSystem(),
				new TopDownSystem(),
				new ReviveSystem(),
				new BotCharacterSystem(),
				new StatSystem(),
				new SpellSystem(),
				new DeathFlagSystem(),
				
				// TODO: Remove when not in tutorial
				new GateSystem(),
				new TriggerSystem(),
				new DestructibleSystem(),
				new TeamSystem(), // needs to be after bots

				// Finalizer systems
				new GameSystem(),
				new EntityLateDestroyerSystem()
			};
			return systems.ToArray();
		}
	}
}