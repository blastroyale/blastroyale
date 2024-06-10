using System;
using System.Collections.Generic;

namespace FirstLight.Server.SDK.Models
{
	/// <summary>
	/// Remote collection item obtained from other sources.
	/// </summary>
	[Serializable]
	public class RemoteCollectionItem
	{
		public string TokenId;
		public string Name;
		public string Description;
		public string Image;
		public List<string> Attributes = new();
	}
}