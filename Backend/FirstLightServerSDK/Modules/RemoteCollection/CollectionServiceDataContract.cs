using System;
using System.Collections.Generic;
using FirstLightServerSDK.Modules;
using MessagePack;

namespace FirstLight.Server.SDK.Models
{
	[Serializable]
	public class CollectionFetchRequest : ICloudScriptDataObject
	{
		public string CollectionName { get; set; }

		public Dictionary<string, string> Data { get; set; }
	}

	[MessagePackObject]
	[Serializable]
	public class CollectionFetchResponse
	{
		[Key(0)] public IEnumerable<RemoteCollectionItem> Owned;
	}
}