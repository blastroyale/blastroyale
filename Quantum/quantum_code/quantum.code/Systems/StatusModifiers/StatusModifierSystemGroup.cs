namespace Quantum.Systems
{
	/// <summary>
	/// This system just groups all the <see cref="Modifier"/> systems together to organize the system scheduling better
	/// </summary>
	public class StatusModifierSystemGroup : SystemGroup
	{
		public StatusModifierSystemGroup() : base("Status Modifier Systems", new RageSystem(), new StunSystem(), new ImmunitySystem())
		
		{
		}
	}
}