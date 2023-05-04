using System.Collections.Generic;

namespace FirstLightServerSDK.Modules
{
	public interface ICloudScriptDataObject
	{
		Dictionary<string, string> Data { get; }
	}
}