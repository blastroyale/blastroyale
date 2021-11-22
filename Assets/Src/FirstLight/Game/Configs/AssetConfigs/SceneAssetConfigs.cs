using FirstLight.AssetImporter;
using FirstLight.Game.Ids;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "SceneAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/SceneAssetConfigs")]
	public class SceneAssetConfigs : AssetConfigsScriptableObject<SceneId, Scene>
	{
		
	}
}