using System;
using System.Collections.Generic;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.Collection
{
	[Serializable, IgnoreServerSerialization]
	public struct WeaponSkinsConfig
	{
		public List<Pair<GameId, WeaponSkinConfigEntry>> MeleeWeapons;
	}

	[Serializable]
	public struct WeaponSkinConfigEntry
	{
		[Required] public AssetReferenceGameObject Prefab;
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