using System.Collections.Generic;
using FirstLight.AssetImporter;
using FirstLight.Game.Ids;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's vfx configurations
	/// </summary>
	[CreateAssetMenu(fileName = "VfxAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/VfxAssetConfigs")]
	public class VfxAssetConfigs : AssetConfigsScriptableObject<VfxId, GameObject>
	{
	}
}