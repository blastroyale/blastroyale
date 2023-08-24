using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Photon.Realtime;
using Quantum;
using Quantum.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable object with the necessary config data to start the <see cref="QuantumRunner"/>
	/// </summary>
	[CreateAssetMenu(fileName = "QuantumRunner Configs", menuName = "ScriptableObjects/QuantumRunner Configs")]
	public class QuantumRunnerConfigs : ScriptableObject
	{
		public static int FixedSeed = 0;
		
		[SerializeField] private RuntimeConfig _runtimeConfig;
		[SerializeField] private DeterministicSessionConfigAsset _deterministicConfigAsset;
		[SerializeField] private PhotonServerSettings _serverSettings;

		/// <inheritdoc cref="DeterministicSessionConfigAsset"/>
		public DeterministicSessionConfigAsset DeterministicSessionConfigAsset => _deterministicConfigAsset;

		/// <inheritdoc cref="PhotonServerSettings"/>
		public PhotonServerSettings PhotonServerSettings => _serverSettings;

		/// <summary>
		/// Marks the Quantum simulation to run in offline or online mode
		/// </summary>
		public bool IsOfflineMode { get; set; } = false;

		/// <summary>
		/// Returns the <see cref="RuntimeConfig"/> used to build the simulation from the client side
		/// </summary>
		public RuntimeConfig RuntimeConfig => _runtimeConfig;

		/// <summary>
		/// Defines the <see cref="RuntimeConfig"/> to set on the Quantum's simulation when starting
		/// </summary>
		public void SetRuntimeConfig(QuantumGameModeConfig gameModeConfig, QuantumMapConfig config,
									 List<string> mutators, int botOverwriteDifficulty)
		{
			var op = Addressables.LoadAssetAsync<MapAsset>($"Maps/{config.Map.ToString()}.asset");
			_runtimeConfig.Seed = FixedSeed != 0 ? FixedSeed : Random.Range(0, int.MaxValue);

			_runtimeConfig.MapId = (int) config.Map;
			_runtimeConfig.Map = op.WaitForCompletion().Settings;
			_runtimeConfig.GameModeId = gameModeConfig.Id;
			_runtimeConfig.Mutators = mutators.ToArray();
			_runtimeConfig.BotOverwriteDifficulty = botOverwriteDifficulty;
		}

		/// <inheritdoc cref="QuantumRunner.StartParameters"/>
		/// <remarks>
		/// Default values to start the Quantum simulation based on the current selected adventure
		/// </remarks>
		public QuantumRunner.StartParameters GetDefaultStartParameters(Room room)
		{
			
			var playersInRoom = room.GetRealPlayerAmount();
			var roomSize = room.GetRealPlayerCapacity();
			var gameMode = DeterministicGameMode.Multiplayer;
			
			if (room.CustomProperties.TryGetValue(GameConstants.Network.ROOM_PROPS_BOTS, out var gameHasBots) &&
				!(bool) gameHasBots)
			{
				roomSize = playersInRoom;
			}

			var quitBehaviour = QuantumNetworkCommunicator.QuitBehaviour.LeaveRoomAndBecomeInactive;
			
			FLog.Verbose($"Starting simulation for {roomSize} players");
			
			if (room.IsOffline)
			{
				gameMode = DeterministicGameMode.Local;
				quitBehaviour = QuantumNetworkCommunicator.QuitBehaviour.None;
			}

			var recordInput = FeatureFlags.GetLocalConfiguration().RecordQuantumInput;

			return new QuantumRunner.StartParameters
			{
				RuntimeConfig = _runtimeConfig,
				DeterministicConfig = _deterministicConfigAsset.Config,
				ReplayProvider = null,
				GameMode = gameMode,
				RunnerId = "DEFAULT",
				QuitBehaviour = quitBehaviour,
				LocalPlayerCount = 1,
				RecordingFlags = recordInput ? RecordingFlags.Input : RecordingFlags.None,
				ResourceManagerOverride = null,
				InstantReplayConfig = default,
				HeapExtraCount = 0,
				PlayerCount = roomSize
			};
		}
	}
}