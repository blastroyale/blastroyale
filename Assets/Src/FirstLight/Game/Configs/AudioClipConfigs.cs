using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct AudioClipConfig
	{
		public AudioId AudioId;
		public List<AssetReferenceT<AudioClip>> AudioClips;
		public float BaseVolume;
		public float BasePitch;
		public float VolumeRandDeviation;
		public float PitchRandDeviation;
		
		public float PlaybackVolume => UnityEngine.Random.Range(BaseVolume - VolumeRandDeviation, 
		                                                        BaseVolume + VolumeRandDeviation);
		public float PlaybackPitch => UnityEngine.Random.Range(BasePitch - PitchRandDeviation, 
		                                                       BasePitch + PitchRandDeviation);
		public int PlaybackClipIndex => UnityEngine.Random.Range(0, AudioClips.Count);
	}
	
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumWeaponConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "AudioClipConfigs", menuName = "ScriptableObjects/Configs/AudioClipConfigs")]
	public class AudioClipConfigs : ScriptableObject, IConfigsContainer<AudioClipConfig>
	{
		[SerializeField] private List<AudioClipConfig> _configs = new List<AudioClipConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<AudioClipConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}