using System;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;

namespace FirstLight.Game.Services.Authentication
{
	/// <summary>
	/// Everything related to players names should be here, it's not a perfect approach but it's a good step of having this centralized
	/// Avoid at all costs formatting the name everywhere else
	/// </summary>
	public static class AuthServiceNameExtensions
	{
		public const char SPACE_CHAR_MATCH = '^';

		/// <summary>
		/// Formats the name received from the unity cloud service, this will replace spaces and trim the numbers 
		/// </summary>
		public static string PrettifyUnityDisplayName(string name)
		{
			name = name.Replace(SPACE_CHAR_MATCH, ' ');
			name = TrimUnityDisplayName(name);
			return name;
		}

		/// <summary>
		/// Formats the name received from  playfab, this will replace spaces and trim the numbers 
		/// </summary>
		public static string PrettifyPlayfabName(string name)
		{
			name = name.Replace(SPACE_CHAR_MATCH, ' ');
			return TrimPlayfabDisplayName(name);
		}

		public static string ReplaceSpacesForSpecialChar(string playerName)
		{
			return playerName?.Replace(' ', SPACE_CHAR_MATCH);
		}

		public static string GetPrettyLocalPlayerName(this IAuthService service,
													  bool replaceSpaces = true,
													  bool showTags = true,
													  bool trimNumbers = true)
		{
			var name = service.RawLocalPlayerName;
			if (name == null) return null;
			if (showTags) name = AddTags(name);
			if (replaceSpaces) name = name.Replace(SPACE_CHAR_MATCH, ' ');
			if (trimNumbers) name = TrimPlayfabDisplayName(name);

			return name;
		}

		private static string AddTags(string name)
		{
			var flags = MainInstaller.Resolve<IGameDataProvider>().PlayerDataProvider.Flags;
			if (flags.HasFlag(PlayerFlags.FLGOfficial))
			{
				name = $"<sprite name=\"FLGBadge\"> {name}";
			}
			else if (flags.HasFlag(PlayerFlags.DiscordMod))
			{
				name = $"<sprite name=\"ModBadge\"> {name}";
			}

			return name;
		}

		/// <summary>
		/// Remove the numbers from unity display name
		/// </summary>
		public static string TrimUnityDisplayName(string playerName)
		{
			if (playerName == null) return null;
			int index = playerName.LastIndexOf("#", StringComparison.Ordinal);
			if (index == -1) return playerName;
			return playerName.Substring(0, index);
		}

		public static string TrimPlayfabDisplayName(string playerName)

		{
			return playerName?[..^5];
		}

		public static string GetLocalPlayerExternalID(this IAuthService authService)
		{
			return GetPrettyLocalPlayerName(authService, true, showTags: false, trimNumbers: false);
		}
	}
}