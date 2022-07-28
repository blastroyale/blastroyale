using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct AudioWeaponConfig
	{
		public GameId Id;
		public AudioId WeaponShotAudioId;
		public float BaseVolume;
		public float BasePitch;
		public float VolumeRandDeviation;
		public float PitchRandDeviation;
	}
	
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumWeaponConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "AudioWeaponConfigs", menuName = "ScriptableObjects/Configs/AudioWeaponConfigs")]
	public class AudioWeaponConfigs : ScriptableObject, IConfigsContainer<AudioWeaponConfig>
	{
		[SerializeField] private List<AudioWeaponConfig> _configs = new List<AudioWeaponConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<AudioWeaponConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}