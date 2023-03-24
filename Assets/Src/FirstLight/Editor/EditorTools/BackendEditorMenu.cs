using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using I2.Loc;
using PlayFab;
using Quantum.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Purchasing;
using Environment = FirstLight.Game.Services.Environment;

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

		private static string _backendPath => $"{Application.dataPath}/../Backend";
		private static string _quantumServerPath => $"{Application.dataPath}/../Quantum/quantum_server/Photon-Server/deploy/Plugins/DeterministicPlugin/bin/";
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

		private static void CopyAssembly(string from, string assemblyName)
		{
			var gameDllPath = $"{from}{assemblyName}";
			var destDll = $"{_backendLibsPath}/{assemblyName}";
			if (!Directory.Exists(_backendLibsPath))
			{
				Directory.CreateDirectory(_backendLibsPath);
			}

			File.Copy(gameDllPath, destDll, true);
		}

		[MenuItem("FLG/Backend/Copy DLLs")]
		public static void MoveBackendDlls()
		{
			// Quantum Dependencies
			CopyAssembly(_quantumLibPath, "quantum.code.dll");
			CopyAssembly(_quantumLibPath, "quantum.core.dll");
			CopyAssembly(_quantumLibPath, "PhotonDeterministic.dll");

			// Script Assembly Dependencies
			CopyAssembly(_unityPath, "FirstLight.DataExtensions.dll");
			CopyAssembly(_unityPath, "FirstLight.Game.Server.dll");
			CopyAssembly(_unityPath, "FirstLight.Game.dll");
			CopyAssembly(_unityPath, "FirstLight.Services.dll");
			CopyAssembly(_unityPath, "PhotonQuantum.dll");

			CopyConfigs(); // also copy configs to ensure everything is updated
			CopyTranslations();
		}

		[MenuItem("FLG/Backend/Generate Quantum Assets")]
		public static void ExportQuantumAssets()
		{
			AssetDBGeneration.Export(_quantumServerPath + "assetDatabase.json");
			Debug.Log("Exported Quantum asset database");
		}

		/// <summary>
		/// Generates and copies a gameConfig.json with needed game configs to be shared to the backend
		/// and moves the config file to the backend.
		/// </summary>
		[MenuItem("FLG/Backend/Copy Server Test Configs")]
		public static async void CopyConfigs()
		{
			var serializer = new ConfigsSerializer();
			var configs = new ConfigsProvider();
			var configsLoader = new GameConfigsLoader(new AssetResolverService());
			Debug.Log("Parsing Configs");
			await Task.WhenAll(configsLoader.LoadConfigTasks(configs));
			var serialiezd = serializer.Serialize(configs, "develop");

			var path = $"{_backendResources}/gameConfig.json";
			File.WriteAllText(path, serialiezd);
			Debug.Log($"Parsed and saved gameConfigs at {path}");
		}

		/// <summary>
		/// Generates and copies a gameTranslations.json to be shared to the backend
		/// and moves the file to the backend directory.
		/// </summary>
		[MenuItem("FLG/Backend/ValidateConfigs")]
		public static async Task TestConfigs()
		{
			FeatureFlags.REMOTE_CONFIGURATION = false;
			var serializer = new ConfigsSerializer();
			var allConfigs = new ConfigsProvider();
			var configsLoader = new GameConfigsLoader(new AssetResolverService());
			await Task.WhenAll(configsLoader.LoadConfigTasks(allConfigs));

			FeatureFlags.REMOTE_CONFIGURATION = true;
			var onlyClientConfigs = new ConfigsProvider();
			await Task.WhenAll(configsLoader.LoadConfigTasks(onlyClientConfigs));

			var serverConfigs = new ConfigsProvider();
			var serializedConfigs = serializer.Serialize(allConfigs, "test");
			serializer.Deserialize(serializedConfigs, serverConfigs);

			var inBoth = new List<Type>();
			foreach (var config in allConfigs.GetAllConfigs().Keys)
			{
				if (serverConfigs.GetAllConfigs().ContainsKey(config))
				{
					if (onlyClientConfigs.GetAllConfigs().ContainsKey(config))
					{
						inBoth.Add(config);
					}
				}
			}

			if (inBoth.Count > 0)
			{
				Debug.Log("Configs in both server & client (bad): ");
				Debug.Log(string.Join(",", inBoth.Select(t => t.Name)));
			}
			else
			{
				Debug.Log("All Good");
			}
			
		}
		
		/// <summary>
		/// Generates and copies a gameTranslations.json to be shared to the backend
		/// and moves the file to the backend directory.
		/// </summary>
		[MenuItem("FLG/Backend/Copy Server Translations")]
		public static  void CopyTranslations()
		{
			var language = "English";
			var terms = new Dictionary<string, string>();
			foreach (var s in LocalizationManager.GetTermsList())
			{
				terms[s] = LocalizationManager.GetTranslation(s, default, default, default, default, default, language);
			}

			var serialized = ModelSerializer.Serialize(terms).Value;
			var path = $"{_backendResources}/gameTranslations.json";
			File.WriteAllText(path, serialized);
			Debug.Log($"Parsed and saved translations at {path}");
		}


#if ENABLE_PLAYFABADMIN_API
		/// <summary>
		/// Uploads the last serialized configuration to dev playfab.
		/// Playfab title is set in the Window -> Playfab -> Editor Extension menu
		/// </summary>
		[MenuItem("FLG/Backend/Upload Configs to Playfab")]
		public static async Task UploadToPlayfab()
		{
			var serializer = new ConfigsSerializer();
			var configs = new ConfigsProvider();
			var configsLoader = new GameConfigsLoader(new AssetResolverService());
			Debug.Log("Parsing Configs");
			await Task.WhenAll(configsLoader.LoadConfigTasks(configs));
			Debug.Log("Getting title data");
			PlayFabShortcuts.GetTitleData("GameConfigVersion", configVersion =>
			{
				var currentVersion = ulong.Parse(configVersion ?? "0");
				var nextVersion = currentVersion + 1;
				var title = PlayFab.PfEditor.PlayFabEditorDataService.ActiveTitle;
				if (title == null)
				{
					EditorUtility.DisplayDialog("Enable Playfab Ext", "Please go to Windows -> Playfab and enable playfab EXT to use this", "Accept", "Forcefully Accept");
					return;
				}

				if (!EditorUtility.DisplayDialog("Confirm Version Update",
												 @$"Update configs from version {currentVersion} to {nextVersion} on environment {title.Name.ToUpper()} {title.Id.ToUpper()}?", "Confirm", "Cancel"))
				{
					return;
				}

				var serialiezd = serializer.Serialize(configs, nextVersion.ToString());

				PlayFabShortcuts.SetTitleData(PlayfabConfigurationProvider.ConfigName, serialiezd);
				PlayFabShortcuts.SetTitleData(PlayfabConfigurationProvider.ConfigVersion, nextVersion.ToString());
				Debug.Log($"Configs uploaded to playfab and version bumped to {nextVersion}");
			});
		}
#endif

		[MenuItem("FLG/Backend/Force Update")]
		private static void ForceUpdate()
		{
			var services = MainInstaller.Resolve<IGameServices>();
			((GameCommandService)services.CommandService).ForceServerDataUpdate();
			Debug.Log("Force Update Sent to Server");
		}

		[MenuItem("FLG/Backend/Use Local Server")]
		private static void UseLocalServer()
		{
			FeatureFlags.GetLocalConfiguration().UseLocalServer = true;
			FeatureFlags.SaveLocalConfig();
			PlayFabSettings.LocalApiServer = "http://localhost:7274";
			Debug.Log("Requests will go to LOCAL server now");
		}
		
	
		[MenuItem("FLG/Backend/Environments/Use DEV")]
		private static void DevServer()
		{
			FeatureFlags.GetLocalConfiguration().EnvironmentOverride = Environment.DEV;
			FeatureFlags.SaveLocalConfig();
			Debug.Log("Environment Set: "+FeatureFlags.GetLocalConfiguration().EnvironmentOverride);
		}
		
		[MenuItem("FLG/Backend/Environments/Use STAGING")]
		private static void StagingServer()
		{
			FeatureFlags.GetLocalConfiguration().EnvironmentOverride = Environment.STAGING;
			FeatureFlags.SaveLocalConfig();
			Debug.Log("Environment Set: "+FeatureFlags.GetLocalConfiguration().EnvironmentOverride);
		}
		
		[MenuItem("FLG/Backend/Environments/PROD")]
		private static void ProdServer()
		{
			FeatureFlags.GetLocalConfiguration().EnvironmentOverride = Environment.PROD;
			FeatureFlags.SaveLocalConfig();
			Debug.Log("Environment Set: "+FeatureFlags.GetLocalConfiguration().EnvironmentOverride);
		}

		[MenuItem("FLG/Backend/Use Remote Server")]
		private static void UseRemoteServer()
		{
			FeatureFlags.GetLocalConfiguration().UseLocalServer = false;
			FeatureFlags.SaveLocalConfig();
			PlayFabSettings.LocalApiServer = null;
			Debug.Log("Requests will go to REMOTE server now");
		}

		[MenuItem("FLG/Backend/Use Remote Configs")]
		private static void UseRemoteConfigs()
		{
			FeatureFlags.GetLocalConfiguration().UseLocalConfigs = false;
			FeatureFlags.SaveLocalConfig();
			Debug.Log("Using Remote Configurations from Playfab");
		}

		[MenuItem("FLG/Backend/Use Local Configs")]
		private static void UseLocalConfigs()
		{
			FeatureFlags.GetLocalConfiguration().UseLocalConfigs = true;
			FeatureFlags.SaveLocalConfig();
			Debug.Log("Using Remote Configurations from Playfab");
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
					catItem.Payouts[0].quantity = (double)item.Consumable.UsageCount;
					catItem.Payouts[0].data = item.CustomData;

					var price = item.VirtualCurrencyPrices["RM"] / 100f;
					catItem.applePriceTier = Mathf.RoundToInt(price);
					catItem.googlePrice = new Price { value = (decimal)price };

					catalog.Add(catItem);
				}

				// Save catalog to json
				var catalogString = ProductCatalog.Serialize(catalog);
				Debug.Log($"Saving catalog: {catalogString}");
				File.WriteAllText(ProductCatalog.kCatalogPath, catalogString);

				Debug.Log("Catalog updated successfully.");
			}, error => { Debug.LogError($"Error updating catalog: {error.ErrorMessage}"); });
		}
#endif
	}
}