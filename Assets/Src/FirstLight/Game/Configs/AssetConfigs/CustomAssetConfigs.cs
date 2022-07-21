using Quantum;
using Sirenix.OdinInspector;
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
		[Required] public AssetReferenceGameObject ErrorCube;
		[Required] public AssetReferenceSprite ErrorSprite;
		[Required] public AssetReferenceT<AudioClip> ErrorClip;
		[Required] public AssetReferenceT<Material> ErrorMaterial;
		[Required] public AssetRefEntityPrototype ConsumablePrototype;

		public QuantumAssetConfigs AssetsConfig
		{
			get => Settings;
			set => Settings = value;
		}
	}
}