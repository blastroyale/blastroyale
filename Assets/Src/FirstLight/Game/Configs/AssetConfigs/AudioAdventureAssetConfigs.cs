using FirstLight.AssetImporter;
using FirstLight.Game.Ids;
using UnityEngine;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all Adventure audio assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "AudioAdventureAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/AudioAdventureAssetConfigs")]
	public class AudioAdventureAssetConfigs : AssetConfigsScriptableObject<AudioId, AudioClip>
	{
	}
}