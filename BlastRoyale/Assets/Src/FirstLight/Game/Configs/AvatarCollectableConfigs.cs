using System;
using FirstLight.Game.Utils;
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
	public class AvatarCollectableConfigs : ScriptableObject, ISingleConfigContainer<AvatarCollectableConfig>
	{
		[SerializeField] public AvatarCollectableConfig Config;

		AvatarCollectableConfig ISingleConfigContainer<AvatarCollectableConfig>.Config
		{
			get => Config;
			set => Config = value;
		}
	}
}