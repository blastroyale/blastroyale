using System.Collections.Generic;

namespace Quantum.Systems
{
	public unsafe class TeamSystem : SystemSignalsOnly, ISignalAllPlayersSpawned
	{
		public void AllPlayersSpawned(Frame f)
		{
			foreach (var kp in TeamHelpers.GetPlayersByTeam(f))
			{
				var teamId = kp.Key;
				var members = kp.Value;
				for (var index = 0; index < members.Count; index++)
				{
					var entity = members[index];
					var component = new TeamMember()
					{
						TeamId = teamId,
						TeamIndex = index
					};
					f.Add(entity, component);
					f.Events.OnTeamAssigned(entity);
				}
			}
		}
	}
}