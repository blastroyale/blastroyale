using System;
using System.Collections.Generic;
using Photon.Deterministic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct MatchmakingAndRoomConfig
	{
		[SerializeField, Required, MinValue(0),InfoBox("How much time the OLD matchmaking room have on the timer before the game starts")]
		public int SecondsToStartOldMatchmakingRoom;

		[SerializeField, Required, MinValue(0),InfoBox("How much time players can select the dropzone after the custom game room locks")]
		public int SecondsToLoadCustomGames;

		[SerializeField, Required, MinValue(0), InfoBox("How much time wait if somebody didn't load after time timer finishes.")]
		public int SecondsLoadingTimeout;
		
		[SerializeField, Required, MinValue(0), InfoBox("Playfab ticket timeout!")]
		public int PlayfabTicketTimeout;
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