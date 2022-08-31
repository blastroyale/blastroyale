using System;

namespace ServerSDK.Services
{
	public enum PluginLogLevel
	{
		INFO, WARN, ERROR, DEBUG
	}
	
	public interface IPluginLogger
	{
		void LogInformation(string msg);

		void LogDebug(string msg);

		void LogError(string msg);

		void LogTrace(string trace);
	}
}