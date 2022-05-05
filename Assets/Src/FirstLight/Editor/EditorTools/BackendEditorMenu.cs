using System.IO;
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
		private static readonly string _quantumLibPath = $"{Application.dataPath}/../Assets/Libs/Photon/Quantum/Assemblies/";

		private static void CopyAssembly(string from, string assemblyName)
		{
			var gameDllPath = $"{from}{assemblyName}";
			var destPath = $"{Application.dataPath}/../Backend/Lib";
			var destDll = $"{destPath}/{assemblyName}";
			if (!Directory.Exists(destPath))
			{
				Directory.CreateDirectory(destPath);
			}
			File.Copy(gameDllPath, destDll, true);
		}

		[MenuItem("First Light Games/Backend/Update DLLs")]
		private static void MoveBackendDlls()
		{
			// Quantum Dependencies
			CopyAssembly(_quantumLibPath, "quantum.code.dll");
			  
			// Script Assembly Dependencies
			CopyAssembly(_unityPath,"FirstLight.DataExtensions.dll");
			CopyAssembly(_unityPath,"FirstLight.Game.dll"); 
			CopyAssembly(_unityPath,"FirstLight.Services.dll");
		}
		
		[MenuItem("First Light Games/Backend/Force Update")]
		private static void ForceUpdate()
		{
			var services = MainInstaller.Resolve<IGameServices>();
			((GameCommandService)services.CommandService).ForceServerDataUpdate();
			Debug.Log("Force Update Sent to Server");
		}
		
		[MenuItem("First Light Games/Backend/Local Server")]
		private static void ToggleLocalServer()
		{
			PlayFabSettings.LocalApiServer = "http://localhost:7274";
			Debug.Log("Requests will go to local server now");
		}
	}
}