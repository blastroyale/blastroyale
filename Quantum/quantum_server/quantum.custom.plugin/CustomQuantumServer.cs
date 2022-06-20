using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Photon.Deterministic.Protocol;
using Photon.Deterministic.Server;

namespace Quantum
{
	public class CustomQuantumServer : DeterministicServer, IDisposable
	{
		DeterministicSessionConfig config;
		RuntimeConfig runtimeConfig;
		readonly Dictionary<String, String> photonConfig;
		private readonly PlayfabService _playfab;

		public CustomQuantumServer(Dictionary<String, String> photonConfig) {
			this.photonConfig = photonConfig;
			_playfab = new PlayfabService(photonConfig);
		}

		public override void OnDeterministicSessionConfig(DeterministicPluginClient client, SessionConfig configData)
		{
			config = configData.Config;
		}

		public override void OnDeterministicRuntimeConfig(DeterministicPluginClient client, Photon.Deterministic.Protocol.RuntimeConfig configData)
		{
			runtimeConfig = RuntimeConfig.FromByteArray(configData.Config);
		}

		/// <summary>
		/// Override method that will validate if a given input for the match is valid or not.
		/// Will work with server-side data to ensure all match data sent by clients are valid.
		/// </summary>
		public override bool OnDeterministicPlayerDataSet(DeterministicPluginClient client, SetPlayerData clientPlayerData)
		{
			var clientPlayer = RuntimePlayer.FromByteArray(clientPlayerData.Data);
			var playfabData = _playfab.GetProfileReadOnlyData(clientPlayer.PlayerId);
			var playerNftData = ModelSerializer.DeserializeFromData<NftEquipmentData>(playfabData);
			var serverHashes = playerNftData.Inventory.Values.Select(e => e.GetHashCode()).ToHashSet();
			foreach (var clientEquip in clientPlayer.Loadout)
			{
				var clientEquiphash = clientEquip.GetHashCode();
				if (!serverHashes.Contains(clientEquiphash))
				{
					Log.Error($"Player {clientPlayer.PlayerId} tried to send equipment {clientEquip.GameId} hash {clientEquiphash} which he does not own");
					return false;

				}
			}
			Log.Info("All good all good");
			return true;
		}

		public void Dispose()
		{

		}

	}
}
