using System;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public struct CollectionEnrichedMessage : IMessage
	{
		public Type DataType;
		public Type CollectionType;
	}
}