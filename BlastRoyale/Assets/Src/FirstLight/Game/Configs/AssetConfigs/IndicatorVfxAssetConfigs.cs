using FirstLight.AssetImporter;
using FirstLight.Game.Ids;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's indicators vfx assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "IndicatorVfxAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/IndicatorVfxAssetConfigs")]
	public class IndicatorVfxAssetConfigs : AssetConfigsScriptableObject<IndicatorVfxId, GameObject>
	{
	}
}