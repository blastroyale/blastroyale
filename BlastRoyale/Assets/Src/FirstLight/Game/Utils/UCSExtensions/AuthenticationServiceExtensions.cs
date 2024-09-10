using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using Unity.Services.Authentication;

namespace FirstLight.Game.Utils.UCSExtensions
{
	/// <summary>
	/// Helpers for the UCS Authentication service.
	/// </summary>
	public static class AuthenticationServiceExtensions
	{
		public const char SPACE_CHAR_MATCH = '_';
		
		/// <summary>
		/// Returns the local players name without the hashtag and numbers.
		/// </summary>
		public static string GetPlayerName(this IAuthenticationService authService, bool trim = true, bool tag = true)
		{
			var flags = MainInstaller.Resolve<IGameDataProvider>().PlayerDataProvider.Flags;
			var name = authService.PlayerName;

			// TODO: Here because we connect to quantum before we have the nickname?
			if (name == null)
			{
				return null;
			}

			if (tag)
			{
				if (flags.HasFlag(PlayerFlags.FLGOfficial))
				{
					name = $"<sprite name=\"FLGBadge\"> {name}";
				}
				else if (flags.HasFlag(PlayerFlags.DiscordMod))
				{
					name = $"<sprite name=\"ModBadge\"> {name}";
				}
			}

			if (trim)
			{
				name = name.TrimPlayerNameNumbers();
			}

			name = name.Replace(SPACE_CHAR_MATCH, ' ');

			return name;
		}
	}
}