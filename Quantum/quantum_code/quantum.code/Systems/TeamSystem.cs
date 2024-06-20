using System;
using System.Collections.Generic;
using System.Linq;
using Quantum.Core;

namespace Quantum.Systems
{
	public unsafe class TeamSystem : SystemSignalsOnly, ISignalAllPlayersSpawned
	{
		private Dictionary<EntityRef, byte> DistributeColors(Frame f, List<EntityRef> entityRefs)
		{
			var colors = new Dictionary<EntityRef, byte>();
			var used = new HashSet<byte>();
			// First pass to collect player colors from the runtime data
			foreach (var entityRef in entityRefs)
			{
				if (!f.Unsafe.TryGetPointer<PlayerCharacter>(entityRef, out var player)) continue;
				if (!player->RealPlayer) continue;
				var color = f.GetPlayerData(player->Player).TeamColor;
				// If the player have the same color as other player do not allow it
				if (used.Contains(color)) continue;
				colors.Add(entityRef, color);
				used.Add(color);
			}

			// Then get available color for other players or bots
			var available = new Queue<byte>();
			var maxColors = (used.Count > 0 ? used.Max() : 0) + entityRefs.Count + 1;
			for (byte color = 0; color <= maxColors; color++)
			{
				if (!used.Contains(color))
				{
					available.Enqueue(color);
				}
			}

			// Then set it
			foreach (var entityRef in entityRefs.Where(entityRef => !colors.ContainsKey(entityRef)))
			{
				colors[entityRef] = available.Dequeue();
			}


			return colors;
		}

		public void AllPlayersSpawned(Frame f)
		{
			var playersByTeam = GetPlayersByTeam(f);
			foreach (var kp in playersByTeam)
			{
				var teamId = kp.Key;
				var members = DistributeColors(f, kp.Value);
				int index = 0;
				foreach (var entry in members)
				{
					var entity = entry.Key;
					var color = entry.Value;

					// Cache team mates 
					f.Add(entity, new TeamMember()
					{
						TeamId = teamId,
						TeamIndex = index,
						Color = color
					}, out var teamMember);
					var set = f.ResolveHashSet(teamMember->TeamMates);
					foreach (var member in members.Keys)
					{
						if (member == entity) continue;
						set.Add(member);
					}

					index++;
				}
			}

			foreach (var kp in playersByTeam)
			{
				foreach (var entity in kp.Value) f.Events.OnTeamAssigned(entity);
			}
		}


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
		public static ushort GetAliveTeamMembersAmount(Frame f, EntityRef entity, bool countKnockedOut)
		{
			ushort ct = 0;
			foreach (var ally in GetTeamMemberEntities(f, entity))
			{
				if (!f.Has<AlivePlayerCharacter>(ally))
				{
					continue;
				}
				if (!countKnockedOut && ReviveSystem.IsKnockedOut(f, ally))
				{
					continue;
				}
				ct++;
			}
			return ct;
		}
		
		/// <summary>
		/// Return PlayerCharacters members of the same team of entity, it doesn't include the entity!
		/// </summary>
		public static EntityRef [] GetTeamMemberEntities(Frame f, EntityRef entity)
		{
			if (!f.Unsafe.TryGetPointer<TeamMember>(entity, out var team))
			{
				return Array.Empty<EntityRef>();
			}
			var allies = f.ResolveHashSet(team->TeamMates);
			var arr = new EntityRef[allies.Count];
			var index = 0;
			foreach (var ally in allies)
			{
				arr[index] = ally;
				index++;
			}
			return arr;
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