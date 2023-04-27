using System;
using MessagePack;

namespace FirstLight.Server.SDK.Models
{
	/// <summary>
	/// Remote collection item obtained from other sources.
	/// </summary>
	[Serializable]
	[MessagePackObject()]
	public class RemoteCollectionItem
	{
		[Key(0)]
		public string Identifier;
	}
}