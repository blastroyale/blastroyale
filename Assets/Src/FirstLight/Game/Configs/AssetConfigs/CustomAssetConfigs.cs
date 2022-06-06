using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing generic not identified custom game asset's configurations
	/// </summary>
	[CreateAssetMenu(fileName = "CustomAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/CustomAssetConfigs")]
	public class CustomAssetConfigs : QuantumAssetConfigsAsset
	{
		public AssetReferenceGameObject ErrorCube;
		public AssetReferenceSprite ErrorSprite;
		public AssetReferenceT<AudioClip> ErrorClip;
		public AssetReferenceT<Material> ErrorMaterial;
		public AssetRefEntityPrototype ConsumablePrototype;
		public AssetRefEntityPrototype WeaponPickUpPrototype;
		public AssetRefEntityPrototype ChestPrototype;
		
		public QuantumAssetConfigs AssetsConfig
		{
			get => Settings;
			set => Settings = value;
		}
	}
}