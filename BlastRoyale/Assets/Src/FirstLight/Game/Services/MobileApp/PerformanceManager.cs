using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using UnityEngine;
using UnityEngine.Rendering;
using Screen = UnityEngine.Device.Screen;
using SystemInfo = UnityEngine.Device.SystemInfo;

namespace FirstLight.Game.Services
{
	public enum DevicePower
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
	
	public static class PerformanceConfig
	{
		public static readonly int MID_MIN_MEMORY_GB = 4;
		public static readonly int HIGH_MIN_MEMORY = 12;	
		
		public static readonly int MID_MIN_CPU_COUNT = 2;
		public static readonly int HIGH_MIN_CPU_COUNT = 3;
		public static readonly float MIN_HIGH_BATTERY = 0.25f;

		public static readonly int HIGH_MIN_GPU_MEMORY_GB = 2;
	}
	
	public class PerformanceManager
	{
		public DevicePower DevicePower { get; private set; }
		public float TotalMemoryGigas { get; private set; }
		public float CPUCount { get; private set; }
		public float DeviceDPI { get; private set; }
		public float CurrentResolutionRatio { get; private set; } = 1f;
		public float GraphicCardMemory { get; private set; }

		private bool _highEnabled = true;
		
		public void Initialize(IReadOnlyDictionary<string, string> featureFlags)
		{
			TotalMemoryGigas = SystemInfo.systemMemorySize / 1024.0f;
			CPUCount = SystemInfo.processorCount;
			GraphicCardMemory = SystemInfo.graphicsMemorySize / 1024.0f;
			DeviceDPI = Screen.dpi;

			if (featureFlags.TryGetValue("HIGH_MODE", out var high) && high == "false")
			{
				_highEnabled = false;
			}
#if UNITY_ANDROID
			CheckAndroidDevicePower();
#elif UNITY_IOS
			CheckIosDevicePower();
#endif
			UpdatePerformance();
			if (DevicePower == DevicePower.High)
			{
				BatteryMonitor().Forget();
			}
			FLog.Info($"Device Power: {DevicePower}, DPI={DeviceDPI}, Resolution={CurrentResolutionRatio}");
		}

		private async UniTaskVoid BatteryMonitor()
		{
			while (DevicePower == DevicePower.High)
			{
				if (SystemInfo.batteryLevel <= PerformanceConfig.MIN_HIGH_BATTERY && SystemInfo.batteryStatus != BatteryStatus.Charging)
				{
					UpdatePower(DevicePower.Mid);
				}
				await UniTask.Delay(TimeSpan.FromMinutes(1));
			}
		}

		public bool CanBeHigh() // always true
		{
			return _highEnabled 
				&& SystemInfo.batteryLevel > PerformanceConfig.MIN_HIGH_BATTERY 
				&& TotalMemoryGigas >= PerformanceConfig.HIGH_MIN_MEMORY
				&& CPUCount >= PerformanceConfig.HIGH_MIN_CPU_COUNT
				&& GraphicCardMemory > PerformanceConfig.HIGH_MIN_GPU_MEMORY_GB;
		}

		public bool CanBeMid()
		{
			return TotalMemoryGigas >= PerformanceConfig.MID_MIN_MEMORY_GB
				&& CPUCount >= PerformanceConfig.MID_MIN_CPU_COUNT;
		}

		private void CheckIosDevicePower() // ios is never low
		{
			if (!CanBeHigh())
			{
				DevicePower = DevicePower.Mid;
			}
			else
			{
				DevicePower = DevicePower.High;
			}
		}

		public void UpdatePower(DevicePower power)
		{
			DevicePower = power;
			UpdatePerformance();
		}
		
		private void CheckAndroidDevicePower()
		{
			if (!CanBeMid())
			{
				DevicePower = DevicePower.Low;
			} else if (!CanBeHigh())
			{
				DevicePower = DevicePower.Mid;
			}
			else
			{
				DevicePower = DevicePower.High;
			}
		}

		private void UpdatePerformance()
		{
			QualitySettings.SetQualityLevel((int) DevicePower);
			FLog.Info($"Updated device power {DevicePower}");
		}
	}
}