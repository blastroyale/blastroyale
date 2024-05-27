using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using JetBrains.Annotations;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This class is a dirty workaround because we don't have a proper skin system yet, this just exists to centralize this information
	/// This should be deleted soon
	/// </summary>
	public class TemporaryPlayerBadges
	{
		public static IList<TemporaryPlayerBadges> CurrentSkins = new TemporaryPlayerBadges[]
		{
			new ()
			{
				Flag = PlayerFlags.FLGOfficial, NameTextSprite = "FLGBadge",
			},
			new ()
			{
				Flag = PlayerFlags.DiscordMod, NameTextSprite = "ModBadge",
			}
		};


		[CanBeNull]
		public static TemporaryPlayerBadges GetBadgeBasedOnFlags(PlayerFlags flags)
		{
			return CurrentSkins.FirstOrDefault(temporarySkin => flags.HasFlag(temporarySkin.Flag));
		}


		public PlayerFlags Flag;
		public string NameTextSprite;

		public string GetSpriteText()
		{
			return $"<sprite name=\"{NameTextSprite}\">";
		}
	}
}