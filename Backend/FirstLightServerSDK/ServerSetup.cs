using System;
using System.Collections.Generic;
using System.Reflection;

namespace FirstLight.Server.SDK
{
	/// <summary>
	/// Server setup class.
	/// Game Clients needs to inherit this object and implement and setup how the server should behave here.
	/// This is the main entrypoint from client to server.
	/// </summary>
	public interface IServerSetup
	{

		/// <summary>
		/// Shall return the assembly that contains all commands classes (that inherits IGameCommand)
		/// </summary>
		public Assembly GetCommandsAssembly();
		
		/// <summary>
		/// Gets all game plugins that shall extend the functionality of the server.
		/// </summary>
		ServerPlugin [] GetPlugins();

		/// <summary>
		/// Returns how the server will implement specific dependencies.
		/// Will replace any pre-defined server ones if present.
		/// </summary>
		Dictionary<Type, Type> SetupDependencies();

		/// <summary>
		/// Get the list of commands which will run after get player data, this only executes on the server
		/// </summary>
		Type[] GetInitializationCommandTypes();
	}
}

