using System;
using System.Collections.Generic;
using System.Linq;

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
			var playersByTeam = TeamHelpers.GetPlayersByTeam(f);
			foreach (var kp in playersByTeam)
			{
				var teamId = kp.Key;
				var members = DistributeColors(f, kp.Value);
				int index = 0;
				foreach (var entry in members)
				{
					var entity = entry.Key;
					var color = entry.Value;
					var component = new TeamMember()
					{
						TeamId = teamId,
						TeamIndex = index,
						Color = color
					};
					f.Add(entity, component);
					index++;
				}
			}

			foreach (var kp in playersByTeam)
			{
				foreach (var entity in kp.Value) f.Events.OnTeamAssigned(entity);
			}
		}
	}
}