using System;
using System.Collections.Generic;
using Photon.Deterministic;
using Photon.Hive.Plugin;

namespace Quantum
{

	class QuantumCustomPluginFactory : IPluginFactory
	{

		public IGamePlugin Create(IPluginHost gameHost, String pluginName, Dictionary<String, String> config, out String errorMsg)
		{
			var server = new CustomQuantumServer(config);
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
			/*
			Log.Init(
				info => { plugin.LogInfo(info); },
				warn => { plugin.LogWarning(warn); },
				error => { plugin.LogError(error); },
				exn => { plugin.LogFatal(exn.ToString()); }
			);
			*/
			Log.Init(
				info => { Console.WriteLine(info); },
				warn => { Console.WriteLine(warn); },
				error => { Console.WriteLine(error); },
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
