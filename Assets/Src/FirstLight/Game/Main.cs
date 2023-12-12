using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.StateMachines;
using FirstLight.Game.TestCases;
using FirstLight.Game.Utils;
using FirstLight.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLight.UiService;
using PlayFab;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering.UI;
using Debug = UnityEngine.Debug;

namespace FirstLight.Game
{
	/// <summary>
	/// The Main entry point of the game
	/// </summary>
	public class Main : MonoBehaviour
	{
		private Coroutine _pauseCoroutine;
		private IGameServices _services;

		private void Awake()
		{
			System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskExceptionLogging;
			Screen.sleepTimeout = SleepTimeout.NeverSleep;
		}

		private void OnDestroy()
		{
			System.Threading.Tasks.TaskScheduler.UnobservedTaskException -= TaskExceptionLogging;
		}

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
			_services?.AnalyticsService?.SessionCalls?.SessionEnd(_services?.QuitReason);
		}

		private static async Task<object> Convert(Task task)
		{
			await task;
			var voidTaskType = typeof(Task<>).MakeGenericType(Type.GetType("System.Threading.Tasks.VoidTaskResult"));
			if (voidTaskType.IsAssignableFrom(task.GetType()))
				throw new InvalidOperationException("Task does not have a return value (" + task.GetType().ToString() + ")");
			var property = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
			if (property == null)
				throw new InvalidOperationException("Task does not have a return value (" + task.GetType().ToString() + ")");
			return property.GetValue(task);
		}


		// Does not work with "async void" - works with "async Task" only
		private void TaskExceptionLogging(object sender, UnobservedTaskExceptionEventArgs e)
		{
			try
			{
				if (sender is Task task)
				{
					var convert = Convert(task);
					var objName = convert.Exception?.ToString();

					Debug.LogError(" Async task Exception happnened " + objName);
				}
				else
				{
					Debug.LogError("Try Exception Logging called");
				}

			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}

			Debug.LogException(e.Exception);
		}
	}
}