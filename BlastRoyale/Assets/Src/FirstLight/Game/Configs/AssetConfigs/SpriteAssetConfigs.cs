using FirstLight.AssetImporter;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's sprite assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "SpriteAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/SpriteAssetConfigs")]
	public class SpriteAssetConfigs : AssetConfigsScriptableObject<GameId, Sprite>
	{
	}
}