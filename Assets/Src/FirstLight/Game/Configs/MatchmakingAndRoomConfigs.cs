using System;
using System.Collections.Generic;
using Photon.Deterministic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct MatchmakingAndRoomConfig
	{
		[SerializeField, Required, MinValue(0),InfoBox("How much time is waited before starting a matchmaking game")]
		public int MatchmakingLoadingTimeout;

		[SerializeField, Required, MinValue(0),InfoBox("How much time players can select the dropzone after the custom game room locks")]
		public int SecondsToLoadCustomGames;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="ResourcePoolConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "MatchmakingAndRoomConfigs", menuName = "ScriptableObjects/Configs/MatchmakingAndRoomConfigs")]
	public class MatchmakingAndRoomConfigs : ScriptableObject, ISingleConfigContainer<MatchmakingAndRoomConfig>
	{
		[SerializeField] private MatchmakingAndRoomConfig _config;

		public MatchmakingAndRoomConfig Config
		{
			get => _config;
			set => _config = value;
		}
	}
}