namespace Quantum.Systems.Spawners
{
	/// <summary>
	/// This system just groups all the <see cref="CollectablePlatformSpawner"/> & <see cref="PlatformSpawner"/>systems
	/// together to organize the system scheduling better
	/// </summary>
	public class PlatformSpawnerSystemGroup : SystemGroup
	{
		public PlatformSpawnerSystemGroup() : base("Platform Spawner Systems",
		                                           new ConsumablePlatformSpawnerSystem(),
		                                           new WeaponPlatformSpawnerSystem())
		{
		}
	}
}