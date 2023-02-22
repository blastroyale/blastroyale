namespace Quantum
{
	public partial struct Destructible
	{
		/// <summary>
		/// Initializes this <see cref="Destructible"/> with all the necessary data
		/// </summary>
		internal void Init(Frame f, EntityRef e)
		{
			var targetable = new Targetable();
			targetable.Team = Constants.TEAM_ID_NEUTRAL;
			targetable.IsUntargetable = true;
			
			f.Add(e, targetable);
			f.Add(e, new Stats(Health, DamagePower, 0, 0, 0, 0, 0, 0, 0, 0));
		}
	}
}