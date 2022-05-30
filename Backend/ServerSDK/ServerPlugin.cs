using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServerSDK.Services;

namespace ServerSDK;

/// <summary>
/// Setup objects necessary for a plugin to initialize.
/// This object holds hard-typed references to what can be accessed by a external plugin.
/// </summary>
public class PluginContext
{
	public readonly PluginEventManager PluginEventManager;
	public readonly ILogger Log;
	public readonly IServerStateService ServerState;
	public readonly IServerMutex PlayerMutex;

	public PluginContext(PluginEventManager evManager, IServiceProvider services)
	{
		PluginEventManager = evManager;
		Log = services.GetService<ILogger>()!;
		ServerState = services.GetService<IServerStateService>()!;
		PlayerMutex = services.GetService<IServerMutex>()!;
	}
}

/// <summary>
/// Abstract class to be extended to implement new server plugins.
/// </summary>
public abstract class ServerPlugin
{
	protected virtual string ReadPluginConfig(string path)
	{
		var url = Environment.GetEnvironmentVariable(path, EnvironmentVariableTarget.Process);
		if (url == null)
			throw new Exception($"{path} Environment Config Plugin not set.");
		return url;
	}

	public abstract void OnEnable(PluginContext context);
}


