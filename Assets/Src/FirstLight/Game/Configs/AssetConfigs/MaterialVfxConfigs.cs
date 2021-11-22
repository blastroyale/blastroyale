using FirstLight.AssetImporter;
using FirstLight.Game.Ids;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's material vfx configurations
	/// </summary>
	[CreateAssetMenu(fileName = "MaterialVfxConfigs", menuName = "ScriptableObjects/AssetConfigs/MaterialVfxConfigs")]
	public class MaterialVfxConfigs : AssetConfigsScriptableObject<MaterialVfxId, Material>
	{
	}
}