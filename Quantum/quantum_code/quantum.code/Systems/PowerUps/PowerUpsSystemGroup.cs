namespace Quantum.Systems
{
	/// <summary>
	/// This system just groups all the Power Up systems
	/// together to organize the system scheduling better
	/// </summary>
	public class PowerUpsSystemGroup : SystemGroup
	{
		public PowerUpsSystemGroup() : base("Platform Spawner Systems", new DiagonalshotSystem(),
		                                    new FrontshotSystem(), new MultishotSystem())
		{
		}
	}
}