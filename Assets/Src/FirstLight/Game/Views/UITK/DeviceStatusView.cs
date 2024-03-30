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
	public class DeviceStatusView : UIView2
	{
		private const long UPDATE_INTERVAL = 1000;

		private const float BATTERY_FULL = 0.7f;
		private const float BATTERY_MEDIUM = 0.3f;

		private const int HIGH_LATENCY = 100;

		private const string USS_LATENCY_HIGH = "latency-label--high";
		private const string USS_SPRITE_BATTERY = "sprite-match__icon-battery-{0}";
		private const string USS_SPRITE_WIFI = "sprite-match__icon-wifi-{0}";

		private IGameServices _gameServices;

		private VisualElement _batteryIcon;
		private VisualElement _wifiIcon;
		private Label _latency;

		private IVisualElementScheduledItem _tickScheduledItem;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_gameServices = MainInstaller.Resolve<IGameServices>();

			_batteryIcon = element.Q("BatteryIcon").Required();
			_wifiIcon = element.Q("WifiIcon").Required();
			_latency = element.Q<Label>("LatencyLabel").Required();
		}

		public override void SubscribeToEvents()
		{
			_tickScheduledItem = Element.schedule.Execute(Tick).Every(UPDATE_INTERVAL);
		}

		public override void UnsubscribeFromEvents()
		{
			// Probably not strictly necessary, but just in case
			_tickScheduledItem.Pause();
		}

		private void Tick()
		{
			UpdateBattery();
			UpdateWifi();
			UpdateLatency();
		}

		private void UpdateLatency()
		{
			int latency = _gameServices.NetworkService.QuantumClient.LoadBalancingPeer.LastRoundTripTime;
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

		private void UpdateWifi()
		{
			// TODO: How? Might not be possible.
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
	}
}