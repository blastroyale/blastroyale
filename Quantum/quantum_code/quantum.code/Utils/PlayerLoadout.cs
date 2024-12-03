using System;
using System.Linq;

namespace Quantum
{
	/// <summary>
	/// Reference point for what a player can use as a loadout ingame.
	/// This is used by Unity to fetch the loadout to instantiate Entity Views
	/// </summary>
	public struct PlayerLoadout
	{
		
		public GameId[] Cosmetics;
		public Equipment Weapon;
		public Equipment[] Equipment;

		/// <summary>
		/// Reads the entire player loadout on the given frame
		/// </summary>
		public static PlayerLoadout GetLoadout(Frame f, EntityRef entity)
		{
			var playerCharacter = f.Get<PlayerCharacter>(entity);
			var playerData = f.Get<CosmeticsHolder>(entity);

			var cosmetics = playerData.Cosmetics != default ? f.ResolveList(playerData.Cosmetics).ToArray() : Array.Empty<GameId>();
			return new PlayerLoadout()
			{
				Equipment = Array.Empty<Equipment>(),
				Weapon = playerCharacter.CurrentWeapon,
				Cosmetics = cosmetics,
			};
		}

		public static GameId[] GetCosmetics(Frame f, EntityRef player)
		{
			if (!f.TryGet<CosmeticsHolder>(player, out var c))
			{
				Log.Warn("No Holder");
				return Array.Empty<GameId>();
			}

			if (!f.TryResolveList(c.Cosmetics, out var list))
			{
				Log.Warn("No List Allocated");
				return Array.Empty<GameId>();
			}
			return list.ToArray();
		}
		
		public static GameId? GetCosmetic(Frame f, EntityRef player, GameIdGroup group)
		{
			if (!f.TryGet<CosmeticsHolder>(player, out var c))
			{
				return null;
			}

			if (!f.TryResolveList(c.Cosmetics, out var list))
			{
				return null;
			}
			return list.FirstOrDefault(c => c.IsInGroup(group));
		}
	}
}