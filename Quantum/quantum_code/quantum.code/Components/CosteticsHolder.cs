using System;
using System.Linq;
using Quantum.Collections;
using Quantum.Core;

namespace Quantum
{
	public unsafe partial struct CosmeticsHolder
	{
		
		public void SetCosmetics(Frame f, GameId[] cosmetics)
		{
			var list = f.ResolveList(Cosmetics);
			list.Clear();
			foreach (var gameId in cosmetics)
			{
				list.Add(gameId);
			}
		}

		public GameId? GetEquipped(Frame f, GameIdGroup group)
		{
			var list =  f.ResolveList(Cosmetics);
			return list.FirstOrDefault(c => c.IsInGroup(group));
		}
	}
}