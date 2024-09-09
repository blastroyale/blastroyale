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
	public struct CharacterSkinsConfig
	{
		public List<CosmeticConfigEntry> Skins;
	}

	[Serializable]
	public struct CosmeticConfigEntry
	{
		[Required] public GameId GameId;
		[Required] public AssetReferenceGameObject Prefab;
		[Required] public AssetReferenceSprite Sprite;
	}

	[CreateAssetMenu(fileName = "CharacterSkinConfigs", menuName = "ScriptableObjects/Configs/Collection/CharacterSkinContainer"),
	 IgnoreServerSerialization]
	public class CharacterSkinConfigs : ScriptableObject, ISingleConfigContainer<CharacterSkinsConfig>
	{
		[SerializeField] private CharacterSkinsConfig _config;

		public CharacterSkinsConfig Config
		{
			get => _config;
			set => _config = value;
		}
	}
}