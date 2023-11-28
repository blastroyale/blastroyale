using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace FirstLight.Game.Configs.Collection
{
	[Serializable, IgnoreServerSerialization]
	public struct WeaponSkinsConfig
	{
		public List<Pair<GameIdGroup, WeaponSkinConfigGroup>> Groups;
	}

	[Serializable]
	public struct WeaponSkinConfigGroup
	{
		[Required] public RuntimeAnimatorController DefaultAnimationOverwrite;
		[Required] public GameId DefaultSkin;
		public List<WeaponSkinConfigEntry> Configs;
		
#if UNITY_EDITOR
		[Button]
		public void FillSprites()
		{
			for (var i = 0; i < Configs.Count; i++)
			{	var skinConfigEntry = Configs[i];
				var path = AssetDatabase.GetAssetPath(skinConfigEntry.Prefab.editorAsset);
				var spritePath = Path.GetDirectoryName(path)+"/sprite_automatic.png";
				skinConfigEntry.Sprite = new AssetReferenceSprite(AssetDatabase.AssetPathToGUID(spritePath));
				Configs[i] = skinConfigEntry;
			}
		}
#endif
	}

	[Serializable]
	public struct WeaponSkinConfigEntry
	{
		[Required] public GameId SkinId;

		[Required, RequirePrefabComponent(typeof(WeaponSkinMonoComponent))]
		public AssetReferenceGameObject Prefab;

		[Required] public AssetReferenceSprite Sprite;
        
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="BattlePassConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "WeaponSkinsConfigContainer", menuName = "ScriptableObjects/Configs/Collection/WeaponSkinsConfigContainer"),
	 IgnoreServerSerialization]
	public class WeaponSkinsConfigContainer : ScriptableObject, ISingleConfigContainer<WeaponSkinsConfig>
	{
		[SerializeField] private WeaponSkinsConfig _config;

		public WeaponSkinsConfig Config
		{
			get => _config;
			set => _config = value;
		}
	}
}