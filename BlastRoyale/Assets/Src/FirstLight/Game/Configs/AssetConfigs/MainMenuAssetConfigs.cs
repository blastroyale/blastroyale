using FirstLight.AssetImporter;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "MainMenuAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/MainMenuAssetConfigs")]
	public class MainMenuAssetConfigs : AssetConfigsScriptableObject<GameId, GameObject>
	{
	}
}