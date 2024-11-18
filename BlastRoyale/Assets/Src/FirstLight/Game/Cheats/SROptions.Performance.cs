using System.ComponentModel;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;

public partial class SROptions
{
	[Category("Performance")] public PerformanceMode CurrentMode => MainInstaller.ResolveServices().GameAppService.PerformanceManager.Mode;
	
	[Category("Performance")]
	public void LowMode()
	{
		MainInstaller.ResolveServices().GameAppService.PerformanceManager.UpdatePerformanceMode(PerformanceMode.Low);
	}
	
	[Category("Performance")]
	public void MidMode()
	{
		MainInstaller.ResolveServices().GameAppService.PerformanceManager.UpdatePerformanceMode(PerformanceMode.Mid);
	}
	
	[Category("Performance")]
	public void HighMode()
	{
		MainInstaller.ResolveServices().GameAppService.PerformanceManager.UpdatePerformanceMode(PerformanceMode.High);
	}
}