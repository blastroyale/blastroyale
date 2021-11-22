using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Adds editor menu to build addressables content.
	/// </summary>
	public static class AddressablesBuilderMenu
	{
		[MenuItem("First Light Games/Addressables/Build Addressables")]
		private static void BuildAddressables()
		{
			AddressableAssetSettings.BuildPlayerContent();
		}
	}
}