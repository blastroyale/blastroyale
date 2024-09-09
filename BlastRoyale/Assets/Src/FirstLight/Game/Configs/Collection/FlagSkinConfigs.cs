using System;
using System.Collections.Generic;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs
{
	[Serializable, IgnoreServerSerialization]
	public struct FlagSkinConfig
	{
		public List<CosmeticConfigEntry> Skins;
	}

	[CreateAssetMenu(fileName = "FlagSkinConfigs", menuName = "ScriptableObjects/Configs/Collection/FlagSkinContainer"),
	 IgnoreServerSerialization]
	public class FlagSkinConfigs : ScriptableObject, ISingleConfigContainer<FlagSkinConfig>
	{
		[SerializeField] private FlagSkinConfig _config;

		public FlagSkinConfig Config
		{
			get => _config;
			set => _config = value;
		}
	}
}