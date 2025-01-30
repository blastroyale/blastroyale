using System.IO;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using UnityEngine;

namespace FirstLight.Editor.Artifacts
{
	public class GameConfigArtifact : IArtifact
	{
		public string GenerateConfigJson()
		{
			var serializer = new ConfigsSerializer();
			var configs = new ConfigsProvider();
			var configsLoader = new GameConfigsLoader(new AssetResolverService(), configs);
			Debug.Log("Parsing Configs");
			configsLoader.LoadConfigEditor();

			return serializer.Serialize(configs, "develop");
		}

		public void CopyTo(string directory)
		{
			var serializedJson = GenerateConfigJson();
			var path = $"{directory}/gameConfig.json";
			File.WriteAllText(path, serializedJson);
			Debug.Log($"Parsed and saved gameConfigs at {path}");
		}
	}
}