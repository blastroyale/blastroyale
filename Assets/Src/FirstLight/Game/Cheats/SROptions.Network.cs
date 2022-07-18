using System.ComponentModel;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;

public partial class SROptions
{
	private static GameObject _profilers;

	public bool IsMultiClient;

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
	public void DisconnectQuantum()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		services.NetworkService.QuantumClient.Disconnect();
	}
	
	
	[Category("Network")]
	public bool EnableCommitRoomLock
	{
		get => FeatureFlags.COMMIT_VERSION_LOCK;
		set
		{
			FeatureFlags.COMMIT_VERSION_LOCK = !FeatureFlags.COMMIT_VERSION_LOCK;
		}
	}
}