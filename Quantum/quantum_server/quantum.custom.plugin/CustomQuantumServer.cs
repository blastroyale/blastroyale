using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Photon.Deterministic.Protocol;
using Photon.Deterministic.Server;
using Photon.Hive.Plugin;
using PlayFab.ServerModels;
using quantum.custom.plugin;

namespace Quantum
{
	/// <summary>
	/// Main class to override custom quantum server behaviour.
	/// Contains all required information and overrides to change base quantum behaviour.
	/// </summary>
	public class CustomQuantumServer : DeterministicServer, IDisposable
	{
		private DeterministicSessionConfig _config;
		private RuntimeConfig _runtimeConfig;
		private readonly Dictionary<String, String> _photonConfig;
		private readonly PhotonPlayfabSDK _photonPlayfab;
		private readonly Dictionary<string, SetPlayerData> _clientPlayerDataBytes;
		
		public CustomQuantumServer(Dictionary<String, String> photonConfig, IPluginHost host) {
			_photonConfig = photonConfig;
			_photonPlayfab = new PhotonPlayfabSDK(photonConfig, host);
			_clientPlayerDataBytes = new Dictionary<string, SetPlayerData>();
		}

		/// <summary>
		/// Called whenever a game session is about to start and client passes down session configuration.
		/// </summary>
		public override void OnDeterministicSessionConfig(DeterministicPluginClient client, SessionConfig configData)
		{
			_config = configData.Config;
		}

		public override void OnDeterministicRuntimeConfig(DeterministicPluginClient client, Photon.Deterministic.Protocol.RuntimeConfig configData)
		{
			_runtimeConfig = RuntimeConfig.FromByteArray(configData.Config);
		}

		/// <summary>
		/// Override method that will block adding any player data from to the relay stream by direct client input.
		/// Will call external services via HTTP to validate if the client input is correct, and only after
		/// verification it will add the RuntimePlayer serialized object to relay BitStream.
		/// </summary>
		public override bool OnDeterministicPlayerDataSet(DeterministicPluginClient client, SetPlayerData clientPlayerData)
		{
			var clientPlayer = RuntimePlayer.FromByteArray(clientPlayerData.Data);
			_clientPlayerDataBytes[clientPlayer.PlayerId] = clientPlayerData;
			_photonPlayfab.GetProfileReadOnlyData(clientPlayer.PlayerId, OnUserDataResponse);
			return false; // denies adding player data to the bitstream when client sends it
		}

		/// <summary>
		/// Callback for receiving player data from playfab.
		/// Will match the data sent from client 
		/// </summary>
		private void OnUserDataResponse(IHttpResponse response, object userState)
		{
			var playfabResponse = _photonPlayfab.HttpWrapper.DeserializePlayFabResponse<GetUserDataResult>(response);
			var playfabData = playfabResponse.Data.ToDictionary(
				entry => entry.Key,
				entry => entry.Value.Value);
			var playerId = response.Request.UserState as string;
			if (playerId == null || !_clientPlayerDataBytes.TryGetValue(playerId, out var setPlayerData))
			{
				Log.Error($"Could not find set player data request for player {playerId}");
				return;
			}
			_clientPlayerDataBytes.Remove(playerId);
			var clientPlayer = RuntimePlayer.FromByteArray(setPlayerData.Data);
			var playerNftData = ModelSerializer.DeserializeFromData<NftEquipmentData>(playfabData);
			var serverHashes = playerNftData.Inventory.Values.Select(e => e.GetHashCode()).ToHashSet();
			foreach (var clientEquip in clientPlayer.Loadout)
			{
				var clientEquiphash = clientEquip.GetHashCode();
				if (!serverHashes.Contains(clientEquiphash))
				{
					Log.Error($"Player {clientPlayer.PlayerId} tried to send equipment {clientEquip.GameId} hash {clientEquiphash} which he does not own");
					return;
				}
			}
			SetDeterministicPlayerData(setPlayerData);
		}

		/// <summary>
		/// Called after a match ends
		/// </summary>
		public void Dispose()
		{
			_clientPlayerDataBytes.Clear();
		}

	}
}
