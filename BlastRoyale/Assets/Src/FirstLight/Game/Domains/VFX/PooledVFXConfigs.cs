using System;
using System.Collections.Generic;
using FirstLight.AssetImporter;
using FirstLight.Game.Ids;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's vfx configurations
	/// </summary>
	[CreateAssetMenu(fileName = "VfxAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/New")]
	[IgnoreServerSerialization]
	public class PooledVFXConfigs : ScriptableObject
	{
		[TableList] public List<VfxConfigEntry> Configs = new ();

		[Serializable]
		public class VfxConfigEntry
		{
			public VfxId Id;
			public AssetReferenceGameObject AssetRef;
			[MinValue(0)] public int InitialPoolCount;
		}
	}
}