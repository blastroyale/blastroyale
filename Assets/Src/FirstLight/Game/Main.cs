﻿using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using FirstLight.FLogger;
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

			StartCoroutine(HeartbeatCoroutine());
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			_services?.MessageBrokerService?.Publish(new ApplicationFocusMessage() { IsFocus = hasFocus });
			if (!hasFocus)
			{
				_services?.DataSaver?.SaveAllData();
			}
		}

		private void OnApplicationPause(bool isPaused)
		{
			if (isPaused)
			{
				_pauseCoroutine = StartCoroutine(EndAppCoroutine());
			}
			else if (_pauseCoroutine != null)
			{
				StopCoroutine(_pauseCoroutine);

				_pauseCoroutine = null;
			}

			_services?.MessageBrokerService?.Publish(new ApplicationPausedMessage { IsPaused = isPaused });
		}

		private void OnApplicationQuit()
		{
			_services?.MessageBrokerService?.Publish(new ApplicationQuitMessage());
			_services?.AnalyticsService?.SessionCalls?.SessionEnd(_services?.QuitReason);
		}

		private void TrySetLocalServer()
		{
#if UNITY_EDITOR
			FeatureFlags.ParseLocalFeatureFlags();
			Debug.Log("Using local server? -" + FeatureFlags.GetLocalConfiguration().UseLocalServer);
#endif
		}

		private IEnumerator EndAppCoroutine()
		{
			// The app is closed after 30 sec of being unused
			yield return new WaitForSeconds(30);

			_services?.QuitGame("App closed after 30 sec of being unused");
		}

		private IEnumerator HeartbeatCoroutine()
		{
			var waitFor30Seconds = new WaitForSeconds(30);
			var waitFor5Seconds = new WaitForSeconds(5);
			

			while (true)
			{
				yield return FLGTestRunner.Instance.IsRunning() ? waitFor5Seconds : waitFor30Seconds;
				_services?.AnalyticsService.SessionCalls.Heartbeat();
			}
		}

		// Does not work with "async void" - works with "async Task" only
		private void TaskExceptionLogging(object sender, UnobservedTaskExceptionEventArgs e)
		{
			if (sender.GetType().GetGenericTypeDefinition() == typeof(Task<>))
			{
				var task = sender as Task<object>;
				var objName = task.Result is UnityEngine.Object ? ((UnityEngine.Object)task.Result).name : task.Result.ToString();

				Debug.LogError($"Task exception sent by the object {objName}");
			}
			else
			{
				Debug.LogError("Exception raised from a `async void` method. Please do not use async void.");
			}

			Debug.LogException(e.Exception);
		}
	}
}