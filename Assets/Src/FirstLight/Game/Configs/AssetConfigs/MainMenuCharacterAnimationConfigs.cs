
using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct MainMenuCharacterAnimationConfig
	{
		public int FlareAnimMinPlaybackTime;
		public int FlareAnimMaxPlaybackTime;
		public string [] AnimationNames;
	}
	
	/// <summary>
	/// Loads up a list of animation names that characters can animate with in the front end.
	/// </summary>
	[CreateAssetMenu(fileName = "MainMenuCharacterAnimationConfigs", menuName = "ScriptableObjects/Configs/MainMenuCharacterAnimationConfigs")]
	public class MainMenuCharacterAnimationConfigs : ScriptableObject, ISingleConfigContainer<MainMenuCharacterAnimationConfig>
	{
		[SerializeField] private MainMenuCharacterAnimationConfig _config;

		public MainMenuCharacterAnimationConfig Config
		{
			get => _config;
			set => _config = value;
		}
	}
}
