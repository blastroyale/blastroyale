using System;

namespace ServerSDK
{
	/// <summary>
	/// Abstract class to be extended to implement new server plugins.
	/// </summary>
	public abstract class ServerPlugin
	{
		protected virtual string ReadPluginConfig(string path)
		{
			return null;
		}

		public abstract void OnEnable(PluginContext context);
	}



}


