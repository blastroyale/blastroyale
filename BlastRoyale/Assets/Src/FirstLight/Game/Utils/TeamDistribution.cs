using System;
using System.Collections.Generic;
using System.Linq;
using Quantum;

namespace FirstLight.Game.Utils
{
	public static class TeamDistribution
	{
		/// <summary>
		/// Algorithm to distribute players between teams, this will try to merge partial teams togeter
		/// </summary>
		/// <param name="playersByTeam"></param>
		/// <returns></returns>
		public static Dictionary<string, string> Distribute(Dictionary<string, string> input, uint maxTeamSize)
		{
			var sorted = new SortedDictionary<string, string>(input);
			SortedDictionary<string, HashSet<string>> playersByTeam = new();

			foreach (var kv in sorted)
			{
				// If the team is null it means the player doesn't have a team (solo game)
				var team = kv.Value ?? kv.Key;
				if (!playersByTeam.TryGetValue(team, out var members))
				{
					members = new();
					playersByTeam[team] = members;
				}

				members.Add(kv.Key);
			}

			List<string> leftToMatch = new List<string>(playersByTeam.Keys);


			var mergedMembers = new Dictionary<string, HashSet<string>>();


			while (leftToMatch.Count > 0)
			{
				var team = leftToMatch.First();
				int size = playersByTeam[team].Count;
				if (size >= maxTeamSize)
				{
					mergedMembers[team] = playersByTeam[team];
					leftToMatch.Remove(team);
					continue;
				}


				var newMembers = new HashSet<string>(playersByTeam[team]);
				if (leftToMatch.Count > 1)
				{
					for (var i = leftToMatch.Count - 1; i > 0; i--)
					{
						// Can merge
						var otherTeam = leftToMatch[i];
						if (otherTeam == team)
						{
							continue;
						}

						int totalSize = newMembers.Count + playersByTeam[otherTeam].Count;
						if (totalSize <= maxTeamSize)
						{
							// Merge
							// Take original party name
							foreach (var otherMember in playersByTeam[otherTeam]) newMembers.Add(otherMember);
							leftToMatch.RemoveAt(i);
							if (maxTeamSize == totalSize)
							{
								break;
							}
						}
					}
				}

				leftToMatch.Remove(team);
				mergedMembers[team] = newMembers;
			}

			var result = new Dictionary<string, string>();
			foreach (var kv in mergedMembers)
			{
				var team = kv.Key;
				var members = kv.Value;
				foreach (var member in members)
				{
					result[member] = team;
				}
			}

			return result;
		}
	}
}