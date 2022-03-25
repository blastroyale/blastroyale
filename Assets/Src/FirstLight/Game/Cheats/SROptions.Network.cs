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
	
	public bool IsMultiClient { get; set; }

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
		GameObject.FindObjectOfType<HomeScreenPresenter>().SendMessage("OnPlayOfflineClicked");
	}
}