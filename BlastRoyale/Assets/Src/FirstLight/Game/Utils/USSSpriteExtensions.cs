using System;
using FirstLight.FLogger;
using Quantum;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Holds converters / extensions to get USS sprite classes.
	/// </summary>
	public static class USSSpriteExtensions
	{
		/// <summary>
		/// Returns the sprite class of the provided GameID, if it exists.
		/// </summary>
		public static string GetUSSSpriteClass(this GameId id)
		{
			if (id.IsInGroup(GameIdGroup.PlayerSkin))
			{
				return $"sprite-home__character-{id.ToString().ToLowerInvariant()}";
			}

			if (id.IsInGroup(GameIdGroup.Glider))
			{
				return $"sprite-home__glider-{id.ToString().ToLowerInvariant()}";
			}

			if (id.IsInGroup(GameIdGroup.DeathMarker))
			{
				return $"sprite-home__flag-{id.ToString().ToLowerInvariant()}";
			}
			
			// Don't need the sprite class for Equipment or Cores or Currency
			if (id.IsInGroup(GameIdGroup.Equipment) || id.IsInGroup(GameIdGroup.Core) || id.IsInGroup(GameIdGroup.Currency))
			{
				return null;
			}

			FLog.Error($"No sprite class found for GameId.{id}");

			return null;
		}
	}
}