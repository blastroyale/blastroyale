using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using BlastRoyaleNFTPlugin;
using FirstLight.Server.SDK;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Src.FirstLight.Server;

namespace Backend.Plugins
{
	/// <summary>
	/// Plugin manager interface to handle registered commands and registered plugins on server.
	/// </summary>
	public interface IPluginManager
	{
		/// <summary>
		/// Gets all registered plugins
		/// </summary>
		public List<ServerPlugin> GetPlugins();

		/// <summary>
		/// 
		/// </summary>
		public Type GetRegisteredCommand(string typeFullName);


		/// <summary>
		/// Get all initialization command types
		/// </summary>
		public Type[] GetInitializationCommands();
	}

	/// <summary>
	/// Responsible for loading and initializing plugins.
	/// </summary>
	public class PluginManager : IPluginManager
	{
		private IServerSetup _serverSetup;

		private List<ServerPlugin> _loadedPlugins = new();
		private List<Assembly> _loadedLibraries = new();

		private Assembly _commandAssembly;

		public Type? GetRegisteredCommand(string fullName) => _commandAssembly.GetType(fullName);
		public Type[] GetInitializationCommands() => _serverSetup.GetInitializationCommandTypes();

		public List<ServerPlugin> GetPlugins() => _loadedPlugins;

		/// <summary>
		/// Loads the server setup configuration given by the client, if provided in assembly.
		/// </summary>
		public void LoadServerSetup(IServiceCollection services)
		{
			Type serverSetup = typeof(FlgServerConfig);
			if (serverSetup == null)
			{
				return;
			}

			var setup = Activator.CreateInstance(serverSetup) as IServerSetup;
			var dependencies = setup.SetupDependencies();
			foreach (var kp in dependencies)
			{
				services.RemoveAll(kp.Key);
				services.AddSingleton(kp.Key, kp.Value);
			}

			_serverSetup = setup;
			_commandAssembly = setup.GetCommandsAssembly();
		}

		/// <summary>
		/// Loads server plugins and perform hooks to PluginSetup
		/// </summary>
		public void LoadPlugins(PluginContext context, string appPath, IServiceCollection services)
		{
			var allPlugins = LoadServerPlugins();
			var clientPlugins = GetClientPlugins(services);
			if (clientPlugins != null)
			{
				allPlugins.AddRange(clientPlugins);
			}

			foreach (var plugin in allPlugins)
			{
				try
				{
					context.Log.LogInformation($"Initializing plugin {plugin.GetType().Name}");
					plugin.OnEnable(context);
				}
				catch (Exception e)
				{
					context.Log.LogError($"Error initializing plugin {plugin.GetType().Name} {e.Message} {e.StackTrace}");
				}
			}
		}

		/// <summary>
		/// Obtains the list of client-provided plugins.
		/// Client plugins might not have simple access to things like environemnt variables
		/// Their intent is to add specific behaviour on server without requiring to modify server code.
		/// </summary>
		private List<ServerPlugin> GetClientPlugins(IServiceCollection services)
		{
			if (_serverSetup == null)
			{
				return null;
			}

			return _serverSetup.GetPlugins()?.ToList();
		}

		/// <summary>
		/// Gets available plugins that are declared only on server.
		/// Plugins declared on server as opposed to client are the ones that have specific
		/// networking requirements, therefore maintained primarily by server engineers.
		/// </summary>
		private List<ServerPlugin> LoadServerPlugins()
		{
			var loadedPlugins = new List<ServerPlugin>();
			loadedPlugins.Add(new BlastRoyalePlugin());
			return loadedPlugins;
		}

		/// <summary>
		/// Finds all plugins in a given folder.
		/// Will add those plugins & libraries to code namespace.
		/// </summary>
		private IEnumerable<ServerPlugin> FindPluginsAndLibraries(string pluginsFolder)
		{
			foreach (var plugin in Directory.GetFiles(pluginsFolder))
			{
				var ctx = new PluginLoadContext(plugin);
				var assembly = ctx.LoadFromAssemblyPath(plugin);
				var pluginInAssembly = FindPlugin(assembly);
				if (pluginInAssembly != null)
				{
					_loadedPlugins.Add(pluginInAssembly);
				}
				else
				{
					_loadedLibraries.Add(assembly);
				}
			}

			return _loadedPlugins;
		}

		/// <summary>
		/// Searches a given assembly for a plugin
		/// </summary>
		private ServerPlugin FindPlugin(Assembly a)
		{
			foreach (var type in a.GetTypes())
			{
				if (typeof(ServerPlugin).IsAssignableFrom(type))
				{
					return Activator.CreateInstance(type) as ServerPlugin;
				}
			}

			return null;
		}
	}

	class PluginLoadContext : AssemblyLoadContext
	{
		private AssemblyDependencyResolver _resolver;

		public PluginLoadContext(string pluginPath)
		{
			_resolver = new AssemblyDependencyResolver(pluginPath);
		}

		protected override Assembly Load(AssemblyName assemblyName)
		{
			string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
			if (assemblyPath != null)
			{
				return LoadFromAssemblyPath(assemblyPath);
			}

			return null;
		}

		protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
		{
			string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
			if (libraryPath != null)
			{
				return LoadUnmanagedDllFromPath(libraryPath);
			}

			return IntPtr.Zero;
		}
	}
}