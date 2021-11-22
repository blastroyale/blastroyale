using FirstLight.AssetImporter;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's sprite assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "DummyAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/DummyAssetConfigs")]
	public class DummyAssetConfigs : AssetConfigsScriptableObject<GameId, GameObject>
	{
	}
}