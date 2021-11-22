using System.Collections.Generic;
using FirstLight.AssetImporter;
using FirstLight.Game.Ids;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all MainMenu audio assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "AudioMainMenuAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/AudioMainMenuAssetConfigs")]
	public class AudioMainMenuAssetConfigs : AssetConfigsScriptableObject<AudioId, AudioClip>
	{
	}
}