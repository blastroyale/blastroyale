using Quantum.Systems;

namespace Quantum 
{
	public static class SystemSetup 
	{
		public static SystemBase[] CreateSystems(RuntimeConfig gameConfig, SimulationConfig simulationConfig) 
		{
			return new SystemBase[] 
			{
				// Initial pre-defined core systems
				new Core.CullingSystem3D(),
				new Core.NavigationSystem(),
				new Core.PlayerConnectedSystem(),
				
				// Initial Systems
				new SystemInitializer(),
				new AiPreUpdateSystem(),
				
				// pre-defined core systems
				new Core.PhysicsSystem3D(),
				new Core.EntityPrototypeSystem(),
				
				// After physics AI system update
				new AiSystem(),
				
				// Signal only systems - only OnInit & Signal order matters
				new MatchDataSystem(),		// Must be the first to guarantee that receives all the events before entities are deleted
				new NavMeshAgentSystem(),
				new StatusModifierSystemGroup(),
				new DummyCharacterSystem(),
				new CollectableSystem(),
				
				// Update systems - Update & OnInit & Signal order matters
				new CommandsSystem(),
				new ShrinkingCircleSystem(),
				new CollectablePlatformSpawnerSystem(),
				new HazardSystem(),
				new ProjectileSystem(),
				new RaycastShotSystem(),
				new PlayerChargingSystem(),
				new PlayerCharacterSystem(),
				new BotCharacterSystem(),
				new StatSystem(),
				new SpellSystem(),
				new TransformOutOfWorldSystem(), // TODO: Remove it when we update Quantum and have Y coordinate in Navmesh
				
				// Finalizer systems
				new GameSystem(),
				new EntityLateDestroyerSystem()
			};
		}
	}
}