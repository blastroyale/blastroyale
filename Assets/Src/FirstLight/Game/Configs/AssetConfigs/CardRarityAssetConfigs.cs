using FirstLight.AssetImporter;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's <see cref="ItemRarity"/> sprite assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "CardRarityAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/CardRarityAssetConfigs")]
	public class CardRarityAssetConfigs : AssetConfigsScriptableObject<ItemRarity, Sprite>
	{
	}
}