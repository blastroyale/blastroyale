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
		public List<AssetReferenceT<AudioClip>> AudioClips;
		public bool Loop;
		public float BaseVolume;
		public float BasePitch;
		public float VolumeRandDeviation;
		public float PitchRandDeviation;
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