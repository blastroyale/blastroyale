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
	public class TemporarySkin
	{
		public static IList<TemporarySkin> CurrentSkins = new TemporarySkin[]
		{
			new TemporarySkin()
			{
				Flag = PlayerFlags.FLGOfficial, HammerAssetAddress = "AdventureAssets/Items/Skins/Hammer/SausageBeater.prefab", NameTextSprite = "FLGBadge",
			},
			new TemporarySkin()
			{
				Flag = PlayerFlags.DiscordMod, HammerAssetAddress = "AdventureAssets/Items/Skins/Hammer/SpickyBoy.prefab", NameTextSprite = "ModBadge",
			}
		};

		[CanBeNull]
		public static TemporarySkin GetSkinBasedOnName(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;
			return CurrentSkins.FirstOrDefault(temporarySkin => name.Contains(temporarySkin.GetSpriteText()));
		}
		

		[CanBeNull]
		public static TemporarySkin GetSkinBasedOnFlags(PlayerFlags flags)
		{
			return CurrentSkins.FirstOrDefault(temporarySkin => flags.HasFlag(temporarySkin.Flag));
		}


		public PlayerFlags Flag;
		public string HammerAssetAddress;
		public string NameTextSprite;

		public string GetSpriteText()
		{
			return $"<sprite name=\"{NameTextSprite}\">";
		}
	}
}