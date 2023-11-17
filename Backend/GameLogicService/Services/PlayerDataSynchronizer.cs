using System;
using System.Collections.Generic;
using FirstLight.Server.SDK.Models;
using Microsoft.Extensions.Logging;

namespace FirstLightServerSDK.Modules
{
	public class PlayerDataSynchronizer : IDataSynchronizer
	{
		public Dictionary<Type, IDataSync> _syncs = new ();
		private ILogger _log;
		
		public PlayerDataSynchronizer(ILogger log)
		{
			_log = log;
		}

		public void RegisterSync(IDataSync sync)
		{
			_syncs.Add(sync.GetType(), sync);
			sync.Register();
			_log.LogInformation("Registered Data Sync "+sync.GetType().Name);
		}
	}
}