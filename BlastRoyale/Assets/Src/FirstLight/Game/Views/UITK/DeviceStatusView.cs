using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Displays device status information such as battery, wifi, and latency.
	/// </summary>
	public class DeviceStatusView : UIView
	{
		private const long UPDATE_INTERVAL = 1000;

		private const float BATTERY_FULL = 0.7f;
		private const float BATTERY_MEDIUM = 0.3f;

		private const int HIGH_LATENCY = 150;
		private const int LATENCY_WINDOW_SIZE = 5;
		private const double LATENCY_MODIFIER = 0.7;

		private const string USS_LATENCY_HIGH = "latency-label--high";
		private const string USS_SPRITE_BATTERY = "sprite-match__icon-battery-{0}";

		private IGameServices _gameServices;

		private VisualElement _batteryIcon;
		private Label _latency;

		private readonly Queue<int> _latencySamples = new (LATENCY_WINDOW_SIZE);

		private IVisualElementScheduledItem _tickScheduledItem;

		protected override void Attached()
		{
			_gameServices = MainInstaller.Resolve<IGameServices>();

			_batteryIcon = Element.Q("BatteryIcon").Required();
			_latency = Element.Q<Label>("LatencyLabel").Required();
			
			_latency.SetDisplay(_gameServices.LocalPrefsService.ShowLatency.Value);
		}

		public override void OnScreenOpen(bool reload)
		{
			_tickScheduledItem = Element.schedule.Execute(Tick).Every(UPDATE_INTERVAL);
		}

		public override void OnScreenClose()
		{
			// Probably not strictly necessary, but just in case
			_tickScheduledItem.Pause();
		}

		private void Tick()
		{
			UpdateBattery();
			UpdateLatency();
		}

		private void UpdateLatency()
		{
			var latency = GetCurrentLatency();

			_latency.text = $"{latency} ms";

			if (latency >= HIGH_LATENCY && !_latency.ClassListContains(USS_LATENCY_HIGH))
			{
				_latency.AddToClassList(USS_LATENCY_HIGH);
			}
			else if (latency < HIGH_LATENCY && _latency.ClassListContains(USS_LATENCY_HIGH))
			{
				_latency.RemoveFromClassList(USS_LATENCY_HIGH);
			}
		}

		private void UpdateBattery()
		{
			string className;
			if (SystemInfo.batteryLevel >= BATTERY_FULL || SystemInfo.batteryStatus == BatteryStatus.Charging)
			{
				className = string.Format(USS_SPRITE_BATTERY, "full");
			}
			else if (SystemInfo.batteryLevel >= BATTERY_MEDIUM)
			{
				className = string.Format(USS_SPRITE_BATTERY, "medium");
			}
			else
			{
				className = string.Format(USS_SPRITE_BATTERY, "low");
			}

			if (!_batteryIcon.ClassListContains(className))
			{
				_batteryIcon.RemoveSpriteClasses();
				_batteryIcon.AddToClassList(className);
			}
		}

		private int GetCurrentLatency()
		{
// TODO: Uncomment when feature is tested
// #if DEVELOPMENT_BUILD || UNITY_EDITOR
// 			return _gameServices.NetworkService.QuantumClient.LoadBalancingPeer.LastRoundTripTime
// #else
			// Latency displayed is calculated as a moving average of the last 5 samples. When the current latency is lower than the average,
			// the average is reset to the current latency. In effect this means that latency drops are reflected immediately, while increases
			// are smoothed out over time.
			var currentLatency = _gameServices.NetworkService.QuantumClient.LoadBalancingPeer.LastRoundTripTime;
			var currentAverage = _latencySamples.Count > 0 ? (int) _latencySamples.Average() : int.MaxValue;

			if (currentLatency < currentAverage)
			{
				_latencySamples.Clear();
				_latencySamples.Enqueue(currentLatency);
			}
			else if (_latencySamples.Count >= LATENCY_WINDOW_SIZE)
			{
				_latencySamples.Dequeue();
				_latencySamples.Enqueue(_gameServices.NetworkService.QuantumClient.LoadBalancingPeer.LastRoundTripTime);
			}
			else
			{
				_latencySamples.Enqueue(_gameServices.NetworkService.QuantumClient.LoadBalancingPeer.LastRoundTripTime);
			}

			// We add a sneaky modifier because players don't like seeing high numbers :)
			return (int) (_latencySamples.Average() * LATENCY_MODIFIER);
//#endif
		}
	}
}