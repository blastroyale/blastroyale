using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using BlastRoyaleNFTPlugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServerSDK;

namespace Backend.Plugins;

/// <summary>
/// Responsible for loading and initializing plugins.
/// </summary>
public class PluginLoader
{
	private List<ServerPlugin> _loadedPlugins = new();
	private List<Assembly> _loadedLibraries = new();
	private ILogger _log;
	
	public PluginLoader(IServiceProvider services)
	{
		_log = services.GetService<ILogger>();
	}

	/// <summary>
	/// Loads server plugins and perform hooks to PluginSetup
	/// </summary>
	public void LoadServerPlugins(PluginContext context, string appPath)
	{
		foreach (var plugin in GetPlugins())
		{
			try
			{
				_log.LogInformation($"Initializing plugin {plugin.GetType().Name}");
				plugin.OnEnable(context);
			}
			catch (Exception e)
			{
				_log.LogError($"Error initializing plugin {plugin.GetType().Name} {e.Message} {e.StackTrace}");
			}
		}
	}
	
	/// <summary>
	/// Gets available plugins.
	/// Currently hacked to hard-code
	/// </summary>
	private List<ServerPlugin> GetPlugins()
	{
		// TODO: Make it work for Azure Functions
		//var loadedPlugins = LoadPlugins(Path.Combine(appPath, "Plugins"));
		var loadedPlugins = new List<ServerPlugin>()
		{
			new BlastRoyaleNftPlugin()
		};
		return loadedPlugins;
	}
	
	/// <summary>
	/// Finds all plugins in a given folder.
	/// Will add those plugins & libraries to code namespace.
	/// </summary>
	private IEnumerable<ServerPlugin> FindPluginsAndLibraries(string pluginsFolder)
	{
		_log.LogDebug($"Loading plugins from {pluginsFolder}");
		foreach (var plugin in Directory.GetFiles(pluginsFolder))
		{
			var ctx = new PluginLoadContext(plugin);
			var assembly = ctx.LoadFromAssemblyPath(plugin);
			var pluginInAssembly = FindPlugin(assembly);
			if (pluginInAssembly != null)
			{
				_log.LogDebug($"Loaded plugin {pluginInAssembly.GetType().Name}");
				_loadedPlugins.Add(pluginInAssembly);
			}
			else
			{
				_log.LogDebug($"Loaded plugin library {assembly.FullName}");
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