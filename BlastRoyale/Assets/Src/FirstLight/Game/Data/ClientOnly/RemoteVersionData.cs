using System;

namespace FirstLight.Game.Data
{
	/// <summary>
	/// This is stored in an remote server so we can remove different environments per app version
	/// </summary>
	[Serializable]
	public class RemoteVersionData
	{
		public string EnvironmentOverwrite;
	}
}