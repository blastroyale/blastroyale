using System.ComponentModel;
using UnityEngine;
using UnityEngine.AddressableAssets;

public partial class SROptions
{
	private static string _privateRoomName = string.Empty;
	private static GameObject _profilers;

	[Category("Quantum")]
	public bool IsPrivateRoomSet => !string.IsNullOrWhiteSpace(PrivateRoomName);

	[Category("Quantum")] public string PrivateRoomName
	{
		get => _privateRoomName;
		set => _privateRoomName = value;
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
}