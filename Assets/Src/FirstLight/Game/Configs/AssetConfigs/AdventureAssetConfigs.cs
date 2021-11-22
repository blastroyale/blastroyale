using FirstLight.AssetImporter;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "AdventureAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/AdventureAssetConfigs")]
	public class AdventureAssetConfigs : AssetConfigsScriptableObject<GameId, GameObject>
	{
	}
}