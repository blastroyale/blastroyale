using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirstLight.Server.SDK.Modules.GameConfiguration
{
	/// <summary>
	/// Interface that represents the service that holds and version configurations
	/// </summary>
	public interface IConfigBackendService
	{
		/// <summary>
		/// Obtains the current version of configuration that is in backend.
		/// Will be performed every request so has to be a fast operation.
		/// </summary>
		public Task<ulong> GetRemoteVersion();

		/// <summary>
		/// Obtains a given configuration from the backend.
		/// </summary>
		public Task<IConfigsProvider> FetchRemoteConfiguration(ulong version);
	}
}