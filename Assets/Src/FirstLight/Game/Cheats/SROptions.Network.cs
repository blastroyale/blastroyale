using System.ComponentModel;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.AddressableAssets;

public partial class SROptions
{
	private static GameObject _profilers;

	public bool IsMultiClient;

	[Category("Photon")]
	public void PrintCurrentServer()
	{
		var client = MainInstaller.ResolveServices().NetworkService.QuantumClient;
		FLog.Info("Current server is " + client.Server + " and region " + client.CloudRegion);
		FLog.Info("Current server state " + client.State + " " + client.MasterServerAddress);
	}


	[Category("Quantum")]
	public void ShowHideStats()
	{
		QuantumStats.GetObject()?.Toggle();
	}

	[Category("Quantum")]
	public void ShowHideProfilers()
	{
		if (_profilers == null)
		{
			var op = Addressables.InstantiateAsync("Profilers");

			_profilers = op.WaitForCompletion();

			return;
		}

		_profilers.SetActive(!_profilers.activeSelf);
	}

	[Category("Quantum")]
	public void StartMultiClientBR()
	{
		IsMultiClient = true;
		GameObject.FindObjectOfType<HomeScreenPresenter>().SendMessage("OnPlayOnlineClicked");
	}

	[Category("Quantum")]
	public void DisconnectQuantumTimeout()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		services.NetworkService.QuantumClient.Disconnect(DisconnectCause.ClientTimeout);
	}

	[Category("Quantum")]
	public void DisconnectQuantumClientLogic()
	{
		// Disconnecting by client logic, as opposed to other methods, actually causes user first to leave the room, 
		// which changes the expected networking behavior.
		var services = MainInstaller.Resolve<IGameServices>();
		services.NetworkService.QuantumClient.Disconnect(DisconnectCause.DisconnectByClientLogic);
	}


	[Category("Network")]
	public bool EnableCommitRoomLock
	{
		get => RemoteConfigs.Instance.CommitVersionLock;
		set => RemoteConfigs.Instance.CommitVersionLock = value;
	}
}