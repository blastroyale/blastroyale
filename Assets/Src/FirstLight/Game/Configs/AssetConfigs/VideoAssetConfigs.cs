using FirstLight.AssetImporter;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Video;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the game's sprite assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "VideoAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/VideoAssetConfigs")]
	public class VideoAssetConfigs : AssetConfigsScriptableObject<GameId, VideoClip>
	{
	}
}