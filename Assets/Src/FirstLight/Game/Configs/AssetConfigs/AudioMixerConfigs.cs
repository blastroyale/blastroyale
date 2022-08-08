using System;
using System.Collections.Generic;
using FirstLight.AssetImporter;
using FirstLight.Game.Ids;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	[Serializable]
	public struct AudioMixerConfig
	{
		public AssetReference AudioMixer;
		public string[] SnapshotKeys;
		public string MixerMasterKey;
		public string MixerSfx2dKey;
		public string MixerSfx3dKey;
		public string MixerBgmKey;
		public string MixerAncrKey;
		public string MixerAmbKey;
	}
	
	/// <summary>
	/// Scriptable object containing all MainMenu audio assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "AudioMixerConfigs", menuName = "ScriptableObjects/AssetConfigs/AudioMixerConfigs")]
	public class AudioMixerConfigs : AssetConfigsScriptableObjectSimple<int, AudioMixerConfig>
	{
	}
}