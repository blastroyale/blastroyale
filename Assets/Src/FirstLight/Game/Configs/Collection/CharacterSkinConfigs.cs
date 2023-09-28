using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.MonoComponent;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace FirstLight.Game.Configs
{
	[Serializable, IgnoreServerSerialization]
	public struct CharacterSkinsConfig
	{
		public List<CharacterSkinConfigEntry> Skins;
		[Required] public AnimatorParams InGameDefaultAnimation;
		[Required] public AnimatorParams MenuDefaultAnimation;

		
	}

	[Serializable]
	public struct AnimatorParams
	{
		[Required] public bool ApplyRootMotion;
		[Required] public AnimatorUpdateMode UpdateMode;
		[Required] public AnimatorCullingMode CullingMode;
		[Required] public RuntimeAnimatorController Controller;
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
	[CreateAssetMenu(fileName = "CharacterSkinConfigs", menuName = "ScriptableObjects/Configs/Collection/CharacterSkinContainer"), IgnoreServerSerialization]
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