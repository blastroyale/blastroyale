using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Authentication;
using Unity.Services.RemoteConfig;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	public class RemoteConfigs
	{
		public readonly bool BetaVersion;

		public struct UserAttributes
		{
		}

		public struct AppAttributes
		{
		}

		public static RemoteConfigs Instance { get; private set; }

		public static async UniTask Init()
		{
			// Remote config requires authentication for managing environment information
			if (!AuthenticationService.Instance.IsSignedIn)
			{
				await AuthenticationService.Instance.SignInAnonymouslyAsync().AsUniTask();
			}

			var rc = await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());

			Instance = JsonConvert.DeserializeObject<RemoteConfigs>(rc.config.ToString());
		}
	}
}