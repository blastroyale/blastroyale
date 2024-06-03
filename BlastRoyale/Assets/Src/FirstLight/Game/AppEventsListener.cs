using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game
{
	/// <summary>
	/// The Main entry point of the game
	/// </summary>
	public class AppEventsListener : MonoBehaviour
	{
		private IGameServices _services;

		private void Start()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			_services?.MessageBrokerService?.Publish(new ApplicationFocusMessage() { IsFocus = hasFocus });
			if (!hasFocus)
			{
				_services?.DataSaver?.SaveData<AppData>();
			}
		}

		private void OnApplicationPause(bool isPaused)
		{
			_services?.MessageBrokerService?.Publish(new ApplicationPausedMessage { IsPaused = isPaused });
		}

		private void OnApplicationQuit()
		{
			_services?.MessageBrokerService?.Publish(new ApplicationQuitMessage());
		}
	}
}