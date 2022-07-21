using FirstLight.AssetImporter;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "EquipmentCollectableEffectsAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/EquipmentCollectableEffectsAssetConfigs")]
	public class EquipmentCollectableEffectsAssetConfigs : AssetConfigsScriptableObject<EquipmentRarity, GameObject>
	{
	}
}