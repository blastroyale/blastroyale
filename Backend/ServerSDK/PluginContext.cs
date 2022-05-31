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