using System;

namespace FirstLightServerSDK.Services
{
	public interface IRemoteConfigProvider
	{
		// Load a config from the server
		public T GetConfig<T>() where T : class;

		public bool ValidateConfig(Type type);
		public int GetConfigVersion();
	}
}