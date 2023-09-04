using System.Collections.Generic;
using Quantum.Core;

namespace Quantum
{
	public static unsafe class TeamHelpers
	{
		/// <summary>
		/// Returns true if the character is in a team. If he's playing solo returns false.
		/// This is regardless if team is alive or not.
		/// </summary>
		public static bool HasTeam(this PlayerCharacter character) => character.TeamId > Constants.TEAM_ID_NEUTRAL;

		public static Dictionary<int, List<EntityRef>> GetPlayersByTeam(Frame f)
		{
			var playersByTeam = new Dictionary<int, List<EntityRef>>();
			foreach (var player in f.Unsafe.GetComponentBlockIterator<PlayerCharacter>())
			{
				var teamId = player.Component->TeamId;
				if (teamId > 0)
				{
					if (!playersByTeam.TryGetValue(teamId, out var entities))
					{
						entities = new List<EntityRef>();
						playersByTeam[teamId] = entities;
					}

					entities.Add(player.Entity);
				}
			}

			return playersByTeam;
		}

		/// <summary>
		/// Return PlayerCharacters members of the same team of entity, it doesn't include the entity!
		/// </summary>
		public static List<EntityComponentPointerPair<PlayerCharacter>> GetTeamMembers(Frame f, EntityRef entity)
		{
			var teamMembers = new List<EntityComponentPointerPair<PlayerCharacter>>();
			if (!f.TryGet<PlayerCharacter>(entity, out var player))
			{
				return teamMembers;
			}

			var playerTeam = player.TeamId;
			foreach (var otherPlayer in f.Unsafe.GetComponentBlockIterator<PlayerCharacter>())
			{
				var teamId = otherPlayer.Component->TeamId;
				if (teamId > 0 && teamId == playerTeam && otherPlayer.Entity != entity)
				{
					teamMembers.Add(otherPlayer);
				}
			}

			return teamMembers;
		}

		/// <summary>
		/// Checks if two entities have a player character, a team, and if their teams are the same
		/// </summary>
		public static bool HasSameTeam(FrameBase f, EntityRef one, EntityRef two)
		{
			return f.TryGet<Targetable>(one, out var viewerPlayer)
				&& f.TryGet<Targetable>(two, out var targetPlayer)
				&& viewerPlayer.Team > 0 && viewerPlayer.Team == targetPlayer.Team;
		}
	}
}