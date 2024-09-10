using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs.Utils;
using Newtonsoft.Json;
using Quantum;

namespace FirstLight.Game.Serializers
{
	public class DurationSerializer : JsonConverter<DurationConfig>
	{
		public static string SPLIT = " to ";

		public override void WriteJson(JsonWriter writer, DurationConfig value, JsonSerializer serializer)
		{
			writer.WriteValue(value.GetStartsAtDateTime().ToString(DurationConfig.DATE_FORMAT) + SPLIT + value.GetEndsAtDateTime().ToString(DurationConfig.DATE_FORMAT));
		}

		public override DurationConfig ReadJson(JsonReader reader, Type objectType, DurationConfig existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var value = (string)reader.Value;
			var split = value?.Split(SPLIT);
			if (value == null || split.Length != 2)
			{
				throw new Exception($"Failed to parse Duration config please use format '{DurationConfig.DATE_FORMAT}{SPLIT}{DurationConfig.DATE_FORMAT}'");
			}

			return new DurationConfig()
			{
				StartsAt = split[0],
				EndsAt = split[1],
			};
		}
	}
}