using FirstLight.AssetImporter;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "EquipmentRarityAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/EquipmentRarityAssetConfigs")]
	public class EquipmentRarityAssetConfigs : AssetConfigsScriptableObject<EquipmentRarity, GameObject>
	{
	}
}