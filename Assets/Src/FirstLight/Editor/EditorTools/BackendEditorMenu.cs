using System.IO;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using PlayFab;
using PlayFab.PfEditor;
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
		private static readonly string _quantumLibPath = $"{Application.dataPath}/../Assets/Libs/Photon/Quantum/Assemblies/";
		
		private static string _backendPath => $"{Application.dataPath}/../Backend";
		private static string _backendLibsPath => $"{_backendPath}/Lib";
		
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
		private static void MoveBackendDlls()
		{
			// Quantum Dependencies
			CopyAssembly(_quantumLibPath, "quantum.code.dll");
			CopyAssembly(_quantumLibPath, "quantum.core.dll");
			CopyAssembly(_quantumLibPath, "PhotonDeterministic.dll");
			  
			// Script Assembly Dependencies
			CopyAssembly(_unityPath,"FirstLight.DataExtensions.dll");
			CopyAssembly(_unityPath,"FirstLight.Game.Server.dll"); 
			CopyAssembly(_unityPath,"FirstLight.Game.dll"); 
			CopyAssembly(_unityPath,"FirstLight.Services.dll");
			CopyAssembly(_unityPath,"PhotonQuantum.dll");
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
			
			File.WriteAllText ($"{_backendPath}/GameLogicService/gameConfig.json", serialiezd);
			Debug.Log("Parsed and saved in backend folder");
		}
		
#if UNITY_EDITOR && ENABLE_PLAYFABADMIN_API		
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
				var title = PlayFabEditorDataService.ActiveTitle;
				if(!EditorUtility.DisplayDialog("Confirm Version Update",
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
	}
}