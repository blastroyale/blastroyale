using Quantum.Systems.Collectables.Consumables;

namespace Quantum.Systems.Collectables
{
	/// <summary>
	/// This system just groups all the <see cref="Collectable"/> systems together to organize the system scheduling better
	/// </summary>
	public class CollectableSystemGroup : SystemGroup
	{
		public CollectableSystemGroup() : base("Collectable Systems", new CollectableSystem(),
		                                       new WeaponCollectableSystem(), new HealthConsumableSystem(), 
		                                       new InterimArmourConsumableSystem(), new RageConsumableSystem(),
		                                       new StashConsumableSystem())
		{
		}
	}
}