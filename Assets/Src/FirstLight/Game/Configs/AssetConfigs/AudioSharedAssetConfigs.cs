using System.Collections.Generic;
using FirstLight.AssetImporter;
using FirstLight.Game.Ids;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all Shared audio assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "AudioSharedAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/AudioSharedAssetConfigs")]
	public class AudioSharedAssetConfigs : AssetConfigsScriptableObject<AudioId, AudioClip>
	{
	}
}