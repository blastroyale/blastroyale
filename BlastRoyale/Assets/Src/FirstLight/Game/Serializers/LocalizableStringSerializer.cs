using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs.Utils;
using Newtonsoft.Json;
using Quantum;

namespace FirstLight.Game.Serializers
{
	public class LocalizableStringSerializer : JsonConverter<LocalizableString>
	{
		public static string LOCALIZATION_KEY_PREFIX = "key:";

		public override void WriteJson(JsonWriter writer, LocalizableString value, JsonSerializer serializer)
		{
			if (value.UseLocalization)
			{
				writer.WriteValue(LOCALIZATION_KEY_PREFIX + value.LocalizationTerm);
			}
			else
			{
				writer.WriteValue(value.Text);
			}
		}

		public override LocalizableString ReadJson(JsonReader reader, Type objectType, LocalizableString existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var value = (string)reader.Value;

			if (value == null)
			{
				return LocalizableString.FromText("MISSING VALUE!");
			}

			if (value.StartsWith(LOCALIZATION_KEY_PREFIX))
			{
				return LocalizableString.FromTerm(value[LOCALIZATION_KEY_PREFIX.Length..]);
			}

			return LocalizableString.FromText(value);
		}
	}
}