using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct AudioWeaponConfig
	{
		public AudioId WeaponShotId;
		public AudioId WeaponShotWindUpId;
		public AudioId WeaponShotWindDownId;
		public AudioId ProjectileFlyTrailId;
		public AudioId ProjectileImpactId;
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
