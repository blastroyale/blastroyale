namespace ServerSDK;


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


