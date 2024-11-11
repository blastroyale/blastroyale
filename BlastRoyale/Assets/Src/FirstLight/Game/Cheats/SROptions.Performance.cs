using System.ComponentModel;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;

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