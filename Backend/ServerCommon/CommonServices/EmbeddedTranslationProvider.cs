using System.Collections.Generic;
using System.IO;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameTranslations;

namespace ServerCommon.CommonServices
{
	/// <inheritdoc/>>
	public class EmbeddedTranslationProvider : ITranslationProvider
	{
		private const string ResourceName = "ServerCommon.Resources.gameTranslations.json";
	
		private Dictionary<string, string> _translations = new();

		public EmbeddedTranslationProvider()
		{
			var content = LoadEmbeddedJson();
			Parse(content);
		}

		/// <inheritdoc/>>
		public string GetTranslation(string key)
		{
			if (_translations.TryGetValue(key, out var translation))
			{
				return translation;
			}

			return key;
		}

		private string LoadEmbeddedJson()
		{
			var assembly = GetType().Assembly;
			using (Stream? stream = assembly.GetManifestResourceStream(ResourceName))
				using (StreamReader reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
		}

		private void Parse(string translationJson)
		{
			_translations = ModelSerializer.Deserialize<Dictionary<string, string>>(translationJson);
		}
	}
}

