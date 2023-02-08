using FirstLight.Game.Utils;
using UnityEditor;

namespace FirstLight.Editor.EditorTools
{
	public static class FlagsEditorMenu
	{
		private const string MenuName = "FLG/Flags/Disable Tutorial";

		public static bool IsTutorialDisabled
		{
			get => FeatureFlags.GetLocalConfiguration().DisableTutorial;
			set
			{
				FeatureFlags.GetLocalConfiguration().DisableTutorial = value;
				FeatureFlags.SaveLocalConfig();
			}
		}

		[MenuItem(MenuName)]
		private static void ToggleDisableTutorial()
		{
			IsTutorialDisabled = !IsTutorialDisabled;
			Menu.SetChecked(MenuName, IsTutorialDisabled);
		}
		
	}
}