using System;
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
	public class NetworkMenu : EditorWindow
	{
		private string _jitter = "0";
		private string _ping = "0";
		private string _loss = "0";
		
		void OnGUI()
		{
			_loss = EditorGUILayout.TextField("Packet Loss %", _loss);
			_jitter = EditorGUILayout.TextField("Set Jitter %", _jitter);
			 _ping = EditorGUILayout.TextField("Set Ping ms", _ping);
			 
			 if (GUILayout.Button("Set"))
			 {
				 var lagSettings = QuantumRunner.Default.NetworkClient.LoadBalancingPeer.NetworkSimulationSettings;
				 lagSettings.IncomingJitter = Int32.Parse(_jitter);
				 lagSettings.OutgoingJitter = Int32.Parse(_jitter);
				 
				 lagSettings.IncomingLag = Int32.Parse(_ping);
				 lagSettings.OutgoingLag = Int32.Parse(_ping);
				 
				 lagSettings.OutgoingLossPercentage = Int32.Parse(_loss);
				 lagSettings.IncomingLossPercentage = Int32.Parse(_loss);
				 
				 Debug.Log("Network Settings Updated");
			 }
		}

		[MenuItem("FLG/Network/Simulate Disconnect")]
		public static void CopyLocalQuantumFiles()
		{
			QuantumRunner.Default.NetworkClient.SimulateConnectionLoss(true);
		}
		
		[MenuItem("FLG/Network/Simulate Network")]
		static void CreateProjectCreationWindow()
		{
			NetworkMenu window = new NetworkMenu();
			window.ShowUtility();
		}

	}
}