using FirstLight.AssetImporter;
using FirstLight.Game.Ids;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using UnityEngine;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all MainMenu audio assets configurations
	/// </summary>
	[IgnoreServerSerialization]
	[CreateAssetMenu(fileName = "AudioSharedAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/AudioSharedAssetConfigs")]
	public class AudioSharedAssetConfigs : AssetConfigsScriptableObjectSimple<AudioId, AudioClipConfig>
	{
	}
}