using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using UnityEngine;

namespace FirstLight.Editor.Artifacts
{
	public class GameConfigArtifact : IArtifact
	{
		public async UniTask<string> GenerateConfigJson()
		{
			var serializer = new ConfigsSerializer();
			var configs = new ConfigsProvider();
			var configsLoader = new GameConfigsLoader(new AssetResolverService());
			Debug.Log("Parsing Configs");
			await UniTask.WhenAll(configsLoader.LoadConfigTasks(configs));
			return serializer.Serialize(configs, "develop");
		}

		public async UniTask CopyTo(string directory)
		{
			var serializedJson = await GenerateConfigJson();
			var path = $"{directory}/gameConfig.json";
			File.WriteAllText(path, serializedJson);
			Debug.Log($"Parsed and saved gameConfigs at {path}");
		}
	}
}