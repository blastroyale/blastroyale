﻿using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstLight.Server.SDK.Modules.PolymorphicMessagePack
{
	//I created this, because I have no clue how to create a delegate (MethodInfo.Invoke is too slow), that uses the generic parameter of the class (As opposed to a generic method that has it's own parameter 'T'). 
	//Is this the only way?
	internal abstract class PolymorphicDelegate
	{
		public abstract void Serialize(ref MessagePackWriter writer, object value, MessagePackSerializerOptions options);
		public abstract object Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options);
	}

	internal class PolymorphicDelegate<T> : PolymorphicDelegate
	{
		private delegate void SerializeDelegate(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options);

		private delegate T DeserializeDelegate(ref MessagePackReader reader, MessagePackSerializerOptions options);

		private SerializeDelegate _serializeDelegate;
		private DeserializeDelegate _deserializeDelegate;

		public PolymorphicDelegate(IFormatterResolver resolver)
		{
			var formatter = resolver.GetFormatter<T>();

			_serializeDelegate = formatter.Serialize;
			_deserializeDelegate = formatter.Deserialize;
		}

		public override void Serialize(ref MessagePackWriter writer, object value, MessagePackSerializerOptions options)
		{
			_serializeDelegate.Invoke(ref writer, (T) value, options);
		}

		public override object Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			return _deserializeDelegate.Invoke(ref reader, options);
		}
	}
}