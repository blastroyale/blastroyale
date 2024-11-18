using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using Screen = UnityEngine.Device.Screen;
using SystemInfo = UnityEngine.Device.SystemInfo;

namespace FirstLight.Game.Services
{
	public enum PerformanceMode
	{
		/// <summary>
		/// Game will look bad, but should run
		/// </summary>
		Low, 
		
		/// <summary>
		/// Game shoud look fine, shoud be our current setup
		/// </summary>
		Mid, 
		
		/// <summary>
		/// Game has capabilities of turning cool shit on
		/// </summary>
		High
	}
	
	public class PerformanceManager
	{
		public PerformanceMode Mode { get; private set; }
		public float TotalMemoryGigas { get; private set; }
		public float CPUCount { get; private set; }
		public float DeviceDPI { get; private set; }
		public float CurrentResolutionRatio { get; private set; } = 1f;
		public float GraphicCardMemory { get; private set; }
		private PerformanceConfig _config;
		
		public void LoadSetup()
		{
			_config = MainInstaller.ResolveData().RemoteConfigProvider.GetConfig<PerformanceConfig>();
			FLog.Verbose("Remote Performance Configs: "+_config);
			TotalMemoryGigas = SystemInfo.systemMemorySize / 1024.0f;
			CPUCount = SystemInfo.processorCount;
			GraphicCardMemory = SystemInfo.graphicsMemorySize / 1024.0f;
			DeviceDPI = Screen.dpi;
			Mode = PerformanceMode.Mid;
			
			var services = MainInstaller.ResolveServices();
			if (services.LocalPrefsService.Performance.Value != -1)
			{
				Mode = (PerformanceMode) services.LocalPrefsService.Performance.Value;
				FLog.Verbose("Loading saved performance mode: "+Mode);
			}
			else
			{
				FLog.Verbose("Evaluating performance specs based on device spec");
				#if UNITY_ANDROID
							Mode = GetAndroidPerformanceMode();
				#elif UNITY_IOS
							Mode = GetIOSPerformanceMode();
				#endif
			}
			UpdateGraphicsQuality();
			FLog.Info($"Performance Mode: {Mode}, DPI={DeviceDPI}, Resolution={CurrentResolutionRatio}");
		}

		public bool CanBeHigh() 
		{
			return SystemInfo.batteryLevel > _config.HighMinBattery 
				&& TotalMemoryGigas >= _config.HighMinMemory
				&& CPUCount >= _config.HighMinCpu
				&& GraphicCardMemory > _config.HighMinGpuMemory;
		}

		public bool CanBeMid()
		{
			return TotalMemoryGigas >= _config.MidMinMemory
				&& CPUCount >= _config.MidMinCpu;
		}

		private PerformanceMode GetIOSPerformanceMode() 
		{
			return !CanBeHigh() ? PerformanceMode.Mid : PerformanceMode.High;
		}

		public void UpdatePerformanceMode(PerformanceMode power)
		{
			Mode = power;
			UpdateGraphicsQuality();
		}
		
		private PerformanceMode GetAndroidPerformanceMode()
		{
			if (!CanBeMid())
			{
				return PerformanceMode.Low;
			}
			return !CanBeHigh() ? PerformanceMode.Mid : PerformanceMode.High;
		}

		private void UpdateGraphicsQuality()
		{
			FLog.Info($"Updating Performance Mode to {Mode}");
			QualitySettings.SetQualityLevel((int) Mode);
			MainInstaller.ResolveServices().LocalPrefsService.Performance.Value = (int) Mode;
		}
	}
}