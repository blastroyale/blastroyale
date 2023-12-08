using UnityEditor;
using UnityEngine.UIElements;

// Register a SettingsProvider using UIElements for the drawing framework:
namespace FirstLight.Editor.EditorTools
{
	internal static class FLGSettingsRegister
	{
		public const string KEY_CUSTOM_PATH = "FLG_CustomPATH";
		public const string KEY_MSBUILD = "FLG_msbuild";
	
		[SettingsProvider]
		public static SettingsProvider CreateMyCustomSettingsProvider()
		{
			// First parameter is the path in the Settings window.
			// Second parameter is the scope of this setting: it only appears in the Settings window for the Project scope.
			var provider = new SettingsProvider("Preferences/FLG", SettingsScope.User)
			{
				label = "FLG",
				// activateHandler is called when the user clicks on the Settings item in the Settings window.
				activateHandler = (searchContext, rootElement) =>
				{
					var properties = new VisualElement
					{
						style =
						{
							flexDirection = FlexDirection.Column
						}
					};
					rootElement.Add(properties);
					
					properties.Add(new Label("Paths"));

					var customPath = new TextField("Custom PATH")
					{
						value = EditorPrefs.GetString(KEY_CUSTOM_PATH)
					};
					customPath.RegisterValueChangedCallback(e => EditorPrefs.SetString(KEY_CUSTOM_PATH, e.newValue));
				
					var msbuild = new TextField("msbuild")
					{
						value = EditorPrefs.GetString(KEY_MSBUILD)
					};
					msbuild.RegisterValueChangedCallback(e => EditorPrefs.SetString(KEY_MSBUILD, e.newValue));
				
					properties.Add(msbuild);
					properties.Add(customPath);
				},

				// Populate the search keywords to enable smart search filtering and label highlighting:
				// keywords = new HashSet<string>(new[] { "Number", "Some String" })
			};

			return provider;
		}
	}
}