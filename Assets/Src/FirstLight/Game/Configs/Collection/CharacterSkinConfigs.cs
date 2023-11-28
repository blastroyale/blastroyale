using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Configs.Utils;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace FirstLight.Game.Configs
{
	public enum AnchorType
	{
		Weapon,
		Helmet,
		Shield,
		Armor,
		Glider,
		Amulet,
	}

	[Serializable, IgnoreServerSerialization]
	public struct CharacterSkinsConfig
	{
		[TabGroup("Skin List")]
		public List<CharacterSkinConfigEntry> Skins;
		[TabGroup("Default Animations")]
		[Required] public AnimatorParams InGameDefaultAnimation;
		[TabGroup("Default Animations")]
		[Required] public AnimatorParams MenuDefaultAnimation;
		[TabGroup("Anchors")]
		public List<Pair<AnchorType,List<AnchorSettings>>> Anchors;
	}

	[Serializable]
	public struct AnchorSettings
	{
		public TransformParams Offset;
		public string AttachToBone;
		public bool AtBeggningOfHierarchy;
	}


	[Serializable]
	public struct CharacterSkinConfigEntry
	{
		[Required] public GameId GameId;

		[Required, RequirePrefabComponent(typeof(CharacterSkinMonoComponent))]
		public AssetReferenceGameObject Prefab;

		[Required] public AssetReferenceSprite Sprite;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="BattlePassConfig"/> sheet data
	/// </summary>
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