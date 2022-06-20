using System.IO;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
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
		private const string USE_LOCAL_SERVER_KEY = "UseLocalServer";
		
		private static readonly string _unityPath = $"{Application.dataPath}/../Library/ScriptAssemblies/";
		private static readonly string _quantumLibPath = $"{Application.dataPath}/../Assets/Libs/Photon/Quantum/Assemblies/";
		
		private static string _backendPath => $"{Application.dataPath}/../Backend";
		private static string _backendLibsPath => $"{_backendPath}/Lib";
		
		static BackendMenu()
		{
			if (!EditorPrefs.HasKey(USE_LOCAL_SERVER_KEY))
			{
				EditorPrefs.SetBool(USE_LOCAL_SERVER_KEY, false);
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
		
		[MenuItem("FLG/Backend/Copy Configs & Dlls")]
		private static void CopyConfigsDlls()
		{
			MoveBackendDlls();
			CopyConfigs();
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
			CopyAssembly(_unityPath,"FirstLight.Game.dll"); 
			CopyAssembly(_unityPath,"FirstLight.Services.dll");
		}

		/// <summary>
		/// Generates and copies a gameConfig.json with needed game configs to be shared to the backend
		/// and moves the config file to the backend.
		/// </summary>
		[MenuItem("FLG/Backend/Copy Configs")]
		public static async void CopyConfigs()
		{
			var serializer = new ConfigsSerializer();
			var configs = new ConfigsProvider();
			var configsLoader = new GameConfigsLoader(new AssetResolverService());
			Debug.Log("Parsing Configs");
			await Task.WhenAll(configsLoader.LoadConfigTasks(configs));
			var serialiezd = serializer.Serialize(configs, "develop");
			
			File.WriteAllText ($"{_backendPath}/Backend/gameConfig.json", serialiezd);
			Debug.Log("Parsed and saved in backend folder");
		}
		
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
			EditorPrefs.SetBool(USE_LOCAL_SERVER_KEY, true);
			PlayFabSettings.LocalApiServer = "http://localhost:7274";
			Debug.Log("Requests will go to LOCAL server now");
		}

		[MenuItem("FLG/Backend/Use Remote Server")]
		private static void UseRemoteServer()
		{
			EditorPrefs.SetBool(USE_LOCAL_SERVER_KEY, false);
			PlayFabSettings.LocalApiServer = null;
			Debug.Log("Requests will go to REMOTE server now");
		}
	}
}