using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using Newtonsoft.Json;
using Unity.Services.RemoteConfig;

// ReSharper disable ConvertToConstant.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable InconsistentNaming

namespace FirstLight.Game.Utils
{
	public class RemoteConfigs
	{
		private const string CCD_CONFIG_KEY = "CCD_CONFIG_KEY";
		private const string CCD_CONFIG_TYPE = "ccd";

		/// <summary>
		/// Shows or hides the BETA tag on home screen.
		/// </summary>
		public bool ShowBetaLabel = false;

		/// <summary>
		/// The number of games played required to show a review prompt
		/// </summary>
		public int ReviewPromptGamesPlayedReq = 4;
		
		/// <summary>
		/// If rooms should be created with a commit lock (only clients on the same commit
		/// can play together).
		/// </summary>
		public bool EnableCommitVersionLock = false;

		/// <summary>
		/// Shows the persistent bug report button.
		/// </summary>
		public bool ShowBugReportButton = true;

		public static RemoteConfigs Instance { get; private set; }

		public static async UniTask Init()
		{
			var rc = await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());
			Instance = JsonConvert.DeserializeObject<RemoteConfigs>(rc.config.ToString());

			// Init configs for CCD Game Overrides
			var rcCCD = await RemoteConfigService.Instance.FetchConfigsAsync(CCD_CONFIG_TYPE, new UserAttributes(), new AppAttributes());
			if (rcCCD.HasKey(CCD_CONFIG_KEY))
			{
				var ccdConfigJson = rcCCD.GetJson(CCD_CONFIG_KEY);
				var ccdConfig = JsonConvert.DeserializeObject<CCDConfig>(ccdConfigJson);

				Utils.CCDConfig.CCDBadgeName = ccdConfig.badgeName;
				Utils.CCDConfig.CCDBucketID = ccdConfig.bucketId;

				FLog.Info("CCD", $"Using CCD override - Badge: {ccdConfig.badgeName}, Bucket: {ccdConfig.bucketId}");
			}
		}

		/// <summary>
		/// Additional user attributes for JEXL expressions.
		/// </summary>
		private struct UserAttributes
		{
		}

		/// <summary>
		/// Additional user attributes for JEXL expressions.
		/// </summary>
		private struct AppAttributes
		{
		}

		/// <summary>
		/// CCD override config.
		/// </summary>
		private struct CCDConfig
		{
			public string bucketId;
			public string bucketName;
			public string badgeName;
		}
	}
}