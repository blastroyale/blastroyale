using Microsoft.Extensions.Logging;
using FirstLight.Server.SDK.Services;

namespace Backend.Game.Services
{
	/// <summary>
	/// Server implementation of a plugin logger.
	/// This is needed so plugins are Microsoft.Logging.Extensions agnostic.
	/// </summary>
	public class ServerPluginLogger : IPluginLogger
	{
		private ILogger _mainLog;
		
		public ServerPluginLogger(ILogger mainLog)
		{
			_mainLog = mainLog;
		}
		public void LogInformation(string msg)
		{
			_mainLog.LogInformation(msg);
		}

		public void LogDebug(string msg)
		{
			_mainLog.LogDebug(msg);
		}

		public void LogError(string msg)
		{
			_mainLog.LogError(msg);
		}

		public void LogTrace(string trace)
		{
			_mainLog.LogTrace(trace);
		}
	}
}