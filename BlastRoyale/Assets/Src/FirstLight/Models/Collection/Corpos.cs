using FirstLight.Server.SDK.Models;
using MessagePack;

namespace FirstLight.Models.Collection
{
	[MessagePackObject]
	public class Corpos : RemoteCollectionItem
	{
		[Key(1)] public bool MasculineBody;
	}
}