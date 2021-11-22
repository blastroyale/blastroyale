using FirstLight.Game.Ids;
using FirstLight.UiService;
using FirstLightEditor.UiService;
using UnityEditor;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// Games custom <see cref="UiConfigsEditor{TSet}"/>
	/// </summary>
	[CustomEditor(typeof(UiConfigs))]
	public class GameUiConfigsEditor : UiConfigsEditor<UiSetId>
	{
	}
}