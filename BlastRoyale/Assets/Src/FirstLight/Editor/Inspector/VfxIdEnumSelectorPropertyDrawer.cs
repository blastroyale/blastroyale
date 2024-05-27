using FirstLight.Game.Ids;
using FirstLightEditor;
using UnityEditor;

namespace FirstLight.Editor.Inspector
{
	/// <summary>
	/// This is the inspector property drawer for the <see cref="GuidId"/> 
	/// </summary>
	[CustomPropertyDrawer(typeof(EnumSelector<VfxId>))]
	public class VfxIdEnumSelectorPropertyDrawer : EnumSelectorPropertyDrawer<VfxId>
	{
	}
}