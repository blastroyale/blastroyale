using FirstLight.AssetImporter;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's projectiles assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "ProjectileAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/ProjectileAssetConfigs")]
	public class ProjectileAssetConfigs : AssetConfigsScriptableObject<GameId, GameObject>
	{
	}
}