using System;
using System.Collections.Generic;
using FirstLightServerSDK.Modules;

namespace FirstLight.Server.SDK.Models
{
	[Serializable]
	public class CollectionFetchRequest : ICloudScriptDataObject
	{
		public string CollectionName { get; set; }

		public Dictionary<string, string> Data { get; set; }
	}
	
	[Serializable]
	public class CollectionFetchResponse
	{
		public Dictionary<string, List<RemoteCollectionItem>> CollectionNFTsOwnedDict;
	}
}