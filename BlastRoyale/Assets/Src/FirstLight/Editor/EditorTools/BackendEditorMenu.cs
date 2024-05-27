using FirstLight.Editor.Artifacts;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using PlayFab;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// Editor menu regarding first light games game backend. 
	/// </summary>
	public static class BackendMenu
	{
		private static readonly string _unityPath = $"{Application.dataPath}/../Library/ScriptAssemblies/";

		private static readonly string _quantumLibPath =
			$"{Application.dataPath}/../Assets/Libs/Photon/Quantum/Assemblies/";

		private static string _backendPath => $"{Application.dataPath}/../../Backend";
		
		private static string _backendLibsPath => $"{_backendPath}/Lib";
		private static string _backendResources => $"{_backendPath}/ServerCommon/Resources";


		static BackendMenu()
		{
			var cfg = FeatureFlags.GetLocalConfiguration();
			if (cfg.UseLocalServer)
			{
				PlayFabSettings.LocalApiServer = "http://localhost:7274";
			}
		}

		[MenuItem("FLG/Backend/Export Asset DB to Quantum Local Server")]
		public static void CopyLocalQuantumFiles()
		{
			var qtnPluginFolder = $"{Application.dataPath}/../Quantum/quantum_server/quantum.custom.plugin/";
			ArtifactCopier.Copy(qtnPluginFolder, ArtifactCopier.QuantumAssetDBArtifact);
		}


		[MenuItem("FLG/Backend/Copy DLLs")]
		public static void MoveBackendDlls()
		{
			ArtifactCopier.Copy(_backendLibsPath, ArtifactCopier.QuantumDlls, ArtifactCopier.GameDlls);

			CopyConfigs(); // also copy configs to ensure everything is updated
			CopyTranslations();
		}

		/// <summary>
		/// Generates and copies a gameConfig.json with needed game configs to be shared to the backend
		/// and moves the config file to the backend.
		/// </summary>
		[MenuItem("FLG/Backend/Copy Server Test Configs")]
		public static void CopyConfigs()
		{
			ArtifactCopier.GameConfigs.CopyTo(_backendResources);
		}

		/// <summary>
		/// Generates and copies a gameTranslations.json to be shared to the backend
		/// and moves the file to the backend directory.
		/// </summary>
		[MenuItem("FLG/Backend/Copy Server Translations")]
		public static void CopyTranslations()
		{
			ArtifactCopier.GameTranslations.CopyTo(_backendResources);
		}

#if ENABLE_PLAYFABADMIN_API
		/// <summary>
		/// Uploads the last serialized configuration to dev playfab.
		/// Playfab title is set in the Window -> Playfab -> Editor Extension menu
		/// </summary>
		[MenuItem("FLG/Backend/Upload Configs to Playfab")]
		public static void UploadToPlayfab()
		{
			var serializer = new ConfigsSerializer();
			var configs = new ConfigsProvider();
			var configsLoader = new GameConfigsLoader(new AssetResolverService());
			Debug.Log("Parsing Configs");
			configsLoader.LoadConfigEditor(configs);
			Debug.Log("Getting title data");
			PlayFabShortcuts.GetTitleData("GameConfigVersion", configVersion =>
			{
				var currentVersion = ulong.Parse(configVersion ?? "0");
				var nextVersion = currentVersion + 1;
				var title = PlayFab.PfEditor.PlayFabEditorDataService.ActiveTitle;
				if (title == null)
				{
					EditorUtility.DisplayDialog("Enable Playfab Ext", "Please go to Windows -> Playfab and enable playfab EXT to use this", "Accept",
						"Forcefully Accept");
					return;
				}

				if (!EditorUtility.DisplayDialog("Confirm Version Update",
						@$"Update configs from version {currentVersion} to {nextVersion} on environment {title.Name.ToUpper()} {title.Id.ToUpper()}?",
						"Confirm", "Cancel"))
				{
					return;
				}

				var serialiezd = serializer.Serialize(configs, nextVersion.ToString());

				PlayFabShortcuts.SetTitleData(PlayfabConfigKeys.ConfigName, serialiezd);
				PlayFabShortcuts.SetTitleData(PlayfabConfigKeys.ConfigVersion, nextVersion.ToString());
				Debug.Log($"Configs uploaded to playfab and version bumped to {nextVersion}");
			});
		}
#endif

		[MenuItem("FLG/Backend/Force Update")]
		private static void ForceUpdate()
		{
			var services = MainInstaller.Resolve<IGameServices>();
			((GameCommandService) services.CommandService).ForceServerDataUpdate();
			Debug.Log("Force Update Sent to Server");
		}

#if ENABLE_PLAYFABADMIN_API
		[MenuItem("FLG/Backend/Update IAP Catalog")]
		private static void UpdateIAPCatalog()
		{
			Debug.Log("Requesting catalog items from PlayFab");
			PlayFabAdminAPI.GetCatalogItems(new PlayFab.AdminModels.GetCatalogItemsRequest {CatalogVersion = "Store"}, result =>
			{
				Debug.Log("Request completed successfully.");
				var catalog = new ProductCatalog
				{
					enableCodelessAutoInitialization = false,
					enableUnityGamingServicesAutoInitialization = true
				};

				foreach (var item in result.Catalog)
				{
					var catItem = new ProductCatalogItem()
					{
						id = item.ItemId
					};

					catItem.AddPayout();
					catItem.Payouts[0].type = ProductCatalogPayout.ProductCatalogPayoutType.Item;
					catItem.Payouts[0].quantity = (double) item.Consumable.UsageCount;
					catItem.Payouts[0].data = item.CustomData;

					var price = item.VirtualCurrencyPrices["RM"] / 100f;
					catItem.applePriceTier = Mathf.RoundToInt(price);
					catItem.googlePrice = new Price {value = (decimal) price};

					catalog.Add(catItem);
				}

				// Save catalog to json
				var catalogString = ProductCatalog.Serialize(catalog);
				Debug.Log($"Saving catalog: {catalogString}");
				File.WriteAllText(ProductCatalog.kCatalogPath, catalogString);

				Debug.Log("Catalog updated successfully.");
			}, error => { Debug.LogError($"Error updating catalog: {error.ErrorMessage}"); });
		}

		[MenuItem("FLG/Backend/Clear Frame Snapshot")]
		private static void OpenCurrentAccount()
		{
			var srv = new DataService();
			var data = srv.LoadData<AppData>();
			data.LastCapturedFrameSnapshot = default;
			srv.SaveData<AppData>();
		}
#endif
	}
}