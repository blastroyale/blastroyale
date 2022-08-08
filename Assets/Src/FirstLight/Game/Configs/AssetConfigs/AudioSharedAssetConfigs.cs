using FirstLight.AssetImporter;
using FirstLight.Game.Ids;
using UnityEngine;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all MainMenu audio assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "AudioSharedAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/AudioSharedAssetConfigs")]
	public class AudioSharedAssetConfigs : AssetConfigsScriptableObjectSimple<AudioId, AudioClipConfig>
	{
	}
}