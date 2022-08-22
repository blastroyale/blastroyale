using System;
using System.Collections.Generic;

namespace ServerSDK
{
	/// <summary>
	/// Server setup class.
	/// Game Clients needs to inherit this object and implement and setup how the server should behave here.
	/// This is the main entrypoint from client to server.
	/// </summary>
	public interface IServerSetup
	{
		ServerPlugin [] GetPlugins();

		/// <summary>
		/// Returns how the server will implement specific dependencies.
		/// Will replace any pre-defined server ones if present.
		/// </summary>
		Dictionary<Type, Type> SetupDependencies();
	}
}

