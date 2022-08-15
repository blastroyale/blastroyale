using FirstLight.AssetImporter;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the match's assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "MatchAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/MatchAssetConfigs")]
	public class MatchAssetConfigs : AssetConfigsScriptableObject<GameId, GameObject>
	{
	}
}