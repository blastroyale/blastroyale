using System.Linq;

namespace Quantum
{
	/// <summary>
	/// Reference point for what a player can use as a loadout ingame.
	/// This is used by Unity to fetch the loadout to instantiate Entity Views
	/// </summary>
	public struct PlayerLoadout
	{
		public GameId Skin;
		public Equipment Weapon;
		public Equipment[] Equipment;
		public GameId Footstep;
		public GameId Glider;
		public GameId Deathmarker;

		/// <summary>
		/// Reads the entire player loadout on the given frame
		/// </summary>
		public static PlayerLoadout GetLoadout(Frame f, EntityRef entity)
		{
			var playerCharacter = f.Get<PlayerCharacter>(entity);
			var playerData = f.GetSingleton<GameContainer>().PlayersData[playerCharacter.Player];
			var loadout = new PlayerLoadout()
			{
				Equipment = playerCharacter.Gear.ToList().Where(g => g.IsValid()).ToArray(),
				Skin = playerData.PlayerSkin,
				Weapon = playerCharacter.CurrentWeapon,
				Footstep = GameId.FootprintDot,
				Glider = playerData.Glider,
				Deathmarker = playerData.PlayerDeathMarker
			};
			return loadout;
		}
	}
}