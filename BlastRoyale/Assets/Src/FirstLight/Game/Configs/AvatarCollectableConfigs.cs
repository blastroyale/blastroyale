using System;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct AvatarCollectableConfig
	{
		public SerializedDictionary<GameId, string> GameIdUrlDictionary;
	}

	/// <summary>
	/// Scriptable Object to store collection of picture profile data
	/// </summary>
	[CreateAssetMenu(fileName = "AvatarCollectableConfigs", menuName = "ScriptableObjects/Configs/AvatarCollectableConfigs")]
	public class AvatarCollectableConfigs : ScriptableObject
	{
		[SerializeField] public AvatarCollectableConfig Config;
	}
}