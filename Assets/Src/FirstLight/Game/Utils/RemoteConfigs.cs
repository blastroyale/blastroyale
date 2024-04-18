using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.RemoteConfig;

// ReSharper disable ConvertToConstant.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable InconsistentNaming

namespace FirstLight.Game.Utils
{
	public class RemoteConfigs
	{
		public bool BetaVersion = false;

		public static RemoteConfigs Instance { get; private set; }

		public static async UniTask Init()
		{
			var rc = await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());
			Instance = JsonConvert.DeserializeObject<RemoteConfigs>(rc.config.ToString());
		}

		private struct UserAttributes
		{
		}

		private struct AppAttributes
		{
		}
	}
}