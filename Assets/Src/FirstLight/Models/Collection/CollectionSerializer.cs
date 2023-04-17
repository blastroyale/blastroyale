using System;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.PolymorphicMessagePack;
using MessagePack;

namespace FirstLight.Models.Collection
{
	/// <summary>
	/// Serializer to serialize polymorphic items using messagepack serializer.
	/// Can handle polymorph so we can use class types for collection item types.
	/// </summary>
	public class CollectionSerializer
	{
		private static readonly MessagePackSerializerOptions _options;

		static CollectionSerializer()
		{
			var settings = new PolymorphicMessagePackSettings(MessagePackSerializerOptions.Standard.Resolver);

			settings.RegisterType<RemoteCollectionItem, RemoteCollectionItem>(0);
			settings.RegisterType<RemoteCollectionItem, Corpos>(1);

			_options = new PolymorphicMessagePackSerializerOptions(settings);
		}


		public static string Serialize(CollectionFetchResponse response)
		{
			var blob = MessagePackSerializer.Typeless.Serialize(response, _options);
			return Convert.ToBase64String(blob);
		}


		public static CollectionFetchResponse Deserialize(string str)
		{
			var bytes = Convert.FromBase64String(str);
			return MessagePackSerializer.Deserialize<CollectionFetchResponse>(bytes, _options);
		}
	}
}