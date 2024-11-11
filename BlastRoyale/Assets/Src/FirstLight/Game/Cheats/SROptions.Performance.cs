using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using Quantum.Commands;
using SRDebugger;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

public partial class SROptions
{
	[Category("Performance")] public DevicePower CurrentMode => MainInstaller.ResolveServices().GameAppService.PerformanceManager.DevicePower;
	
	[Category("Performance")]
	public void LowMode()
	{
		MainInstaller.ResolveServices().GameAppService.PerformanceManager.UpdatePower(DevicePower.Low);
	}
	
	[Category("Performance")]
	public void MidMode()
	{
		MainInstaller.ResolveServices().GameAppService.PerformanceManager.UpdatePower(DevicePower.Mid);
	}
	
	[Category("Performance")]
	public void HighMode()
	{
		MainInstaller.ResolveServices().GameAppService.PerformanceManager.UpdatePower(DevicePower.High);
	}
}