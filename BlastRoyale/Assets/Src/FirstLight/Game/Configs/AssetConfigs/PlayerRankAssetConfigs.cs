using System.Collections.Generic;
using FirstLight.AssetImporter;
using FirstLight.Game.Ids;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's sprite assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "PlayerRankAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/PlayerRankAssetConfigs")]
	public class PlayerRankAssetConfigs : AssetConfigsScriptableObject<int, Sprite>
	{
	}
}