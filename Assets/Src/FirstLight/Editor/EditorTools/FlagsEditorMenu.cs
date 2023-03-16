using FirstLight.Game.Utils;
using UnityEditor;

namespace FirstLight.Editor.EditorTools
{
	public static class FlagsEditorMenu
	{
		private const string DisableTutorial = "FLG/Flags/Disable Tutorial";
		private const string ForceHasNfts = "FLG/Flags/Force Has NFTs";


		static FlagsEditorMenu()
		{
			EditorApplication.delayCall += () =>
			{
				Menu.SetChecked(DisableTutorial, IsTutorialDisabled);
				Menu.SetChecked(ForceHasNfts, IsForceHasNfts);
				
			};
		}

		public static bool IsTutorialDisabled
		{
			get => FeatureFlags.GetLocalConfiguration().DisableTutorial;
			set
			{
				FeatureFlags.GetLocalConfiguration().DisableTutorial = value;
				FeatureFlags.SaveLocalConfig();
			}
		}
		
		public static bool IsForceHasNfts
		{
			get => FeatureFlags.GetLocalConfiguration().ForceHasNfts;
			set
			{
				FeatureFlags.GetLocalConfiguration().ForceHasNfts = value;
				FeatureFlags.SaveLocalConfig();
			}
		}


		[MenuItem(DisableTutorial)]
		private static void ToggleDisableTutorial()
		{
			IsTutorialDisabled = !IsTutorialDisabled;
			EditorApplication.delayCall += () => { Menu.SetChecked(DisableTutorial, IsTutorialDisabled); };
		}
		
		[MenuItem(ForceHasNfts)]
		private static void ToggleForceHasNfts()
		{
			IsForceHasNfts = !IsForceHasNfts;
			EditorApplication.delayCall += () => { Menu.SetChecked(ForceHasNfts, IsForceHasNfts); };
		}
	}
}