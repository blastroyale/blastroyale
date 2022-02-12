using System.ComponentModel;
using UnityEngine;
using UnityEngine.AddressableAssets;

public partial class SROptions
{
	private static GameObject _profilers;

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
}