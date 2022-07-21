using System;
using System.Collections.Generic;
using Photon.Deterministic;
using Photon.Hive.Plugin;

namespace Quantum
{
	/// <summary>
	/// Main entry point for the quantum plugin instantiation.
	/// The namespace reference for this factory is in quantum.plugin.cfg in the deploy folder
	/// This class is responsible for creating the plugin everytime a match starts.
	/// </summary>
	public class QuantumCustomPluginFactory : IPluginFactory
	{
		/// <summary>
		/// Called everytime a match starts to setup our custom plugin. Configurations are driven from
		/// plugin.config.cfg in deployment folder.
		/// </summary>
		public IGamePlugin Create(IPluginHost gameHost, String pluginName, Dictionary<String, String> config, out String errorMsg)
		{
			var server = new CustomQuantumServer(config, gameHost);
			var plugin = new CustomQuantumPlugin(server);
			InitLog(plugin);
			if (plugin.SetupInstance(gameHost, config, out errorMsg))
			{
				return plugin;
			}
			return null;
		}

		private void InitLog(DeterministicPlugin plugin)
		{
			Log.Init(
				info => { plugin.LogInfo(info); },
				warn => { plugin.LogWarning(warn); },
				error => { plugin.LogError(error); },
				exn => { plugin.LogFatal(exn.ToString()); }
			);
			DeterministicLog.Init(
				info => { plugin.LogInfo(info); },
				warn => { plugin.LogWarning(warn); },
				error => { plugin.LogError(error); },
				exn => { plugin.LogFatal(exn.ToString()); }
			);
		}
	}
}
