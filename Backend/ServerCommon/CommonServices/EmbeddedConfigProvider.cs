using System;
using System.Collections;
using System.IO;
using FirstLight.Server.SDK.Modules.GameConfiguration;

namespace ServerCommon.CommonServices
{
	public class EmbeddedConfigProvider : ConfigsProvider
	{
		private const string ResourceName = "ServerCommon.Resources.gameConfig.json";


		public EmbeddedConfigProvider()
		{
			var serializer = new ConfigsSerializer();
			var content = LoadEmbeddedJson();
			serializer.Deserialize(content, this);
		}

		/// <summary>
		/// Obtains the config by a hard-typed type as opposed to a generic type
		/// </summary>
		public IEnumerable GetConfigByType(Type type)
		{
			GetAllConfigs().TryGetValue(type, out var cfg);
			return cfg;
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
	}
}

