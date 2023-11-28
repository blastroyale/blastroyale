using System.Collections.Generic;
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
			var playerData = f.GetSingleton<GameContainer>().PlayersData[playerCharacter.Player];

			var cosmetics = playerData.Cosmetics != default ? f.ResolveList(playerData.Cosmetics).ToArray() : new GameId[] { };
			return new PlayerLoadout()
			{
				Equipment = playerCharacter.Gear.ToList().Where(g => g.IsValid()).ToArray(),
				Weapon = playerCharacter.CurrentWeapon,
				Cosmetics = cosmetics,
			};
		}


		public static GameId[] GetCosmetics(Frame f, PlayerRef player)
		{
			var playerData = f.GetSingleton<GameContainer>().PlayersData[player];
			if (playerData.Cosmetics == default) return new GameId[] { };
			return f.ResolveList(playerData.Cosmetics).ToArray();
		}
	}
}