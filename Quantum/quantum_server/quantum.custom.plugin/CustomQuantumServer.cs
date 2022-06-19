using System;
using System.Collections.Generic;
using System.IO;
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

		public CustomQuantumServer(Dictionary<String, String> photonConfig) { this.photonConfig = photonConfig; }

		// here we're just caching the match configs (Deterministic and Runtime) for the authoritative simulation (match result validation)
		// optionally, we could modify the passed parameters so to have the configs defined on the server itself
		public override void OnDeterministicSessionConfig(DeterministicPluginClient client, SessionConfig configData)
		{
			config = configData.Config;
		}

		// same as comment on method above
		public override void OnDeterministicRuntimeConfig(DeterministicPluginClient client, Photon.Deterministic.Protocol.RuntimeConfig configData)
		{
			runtimeConfig = RuntimeConfig.FromByteArray(configData.Config);
		}

		public override bool OnDeterministicPlayerDataSet(DeterministicPluginClient client, SetPlayerData playerData)
		{
			return false;
		}

		public void Dispose()
		{

		}

	}
}
