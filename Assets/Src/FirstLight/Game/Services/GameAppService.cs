using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.StateMachines;
using FirstLight.Game.TestCases;
using FirstLight.Game.Utils;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Handles game application proccess
	/// </summary>
	public interface IGameAppService
	{
	}

	public class GameAppService : IGameAppService
	{
		private IGameServices _services;
		private static readonly TimeSpan _maxPauseTime = TimeSpan.FromMinutes(5);
		private static readonly TimeSpan _heartBeatTest = TimeSpan.FromSeconds(5);
		private static readonly TimeSpan _heartBeat = TimeSpan.FromSeconds(30);
		private DateTime _pauseTime;
		private UniTask _heartbeatTask;
		private bool _paused;

		public GameAppService(IGameServices services)
		{
			if (!FeatureFlags.GetLocalConfiguration().DisablePauseBehaviour)
			{
				Application.runInBackground = false;
			}

			_services = services;
			_services.MessageBrokerService.Subscribe<ApplicationFocusMessage>(OnApplicationFocus);
			_services.MessageBrokerService.Subscribe<ApplicationPausedMessage>(OnApplicationPause);
			_heartbeatTask = HeartbeatTask();
		}

		private async UniTask HeartbeatTask()
		{
			while (true)
			{
				_services?.AnalyticsService.SessionCalls.Heartbeat();
				await UniTask.Delay(FLGTestRunner.Instance.IsRunning() ? _heartBeatTest : _heartBeat);
			}
		}

		private void HandleGamePaused()
		{
			if (!_services.AuthenticationService.State.LoggedIn) return;
			if (_paused) return;
			_paused = true;
			if (FeatureFlags.PAUSE_FREEZE)
			{
				Time.timeScale = 0;
			}
			_pauseTime = DateTime.UtcNow;
			FLog.Info("Game Paused");
		}

		private void HandleGameUnpaused()
		{
			if (!_services.AuthenticationService.State.LoggedIn || _services.NetworkService.QuantumClient == null) return;
			if (!Application.isPlaying) return;
#if UNITY_EDITOR
			if (EditorApplication.isPaused) return;
#endif
			if (!_paused) return;
			if (DateTime.UtcNow - _pauseTime > _maxPauseTime)
			{
				FLog.Warn("Max pause time elapsed");
				_services.GenericDialogService.OpenSimpleMessage("Disconnected", "Please Restart", Application.Quit);
				return;
			}
			if (FeatureFlags.PAUSE_FREEZE)
			{
				Time.timeScale = 1;
			}
			_paused = false;
			FLog.Info("Game Resumed");
		}

		private void OnPause(bool paused)
		{
			if (MainInstaller.TryResolve<IGameStateMachine>(out var state))
			{
				FLog.Info($"Game Paused Update: {paused} {state.GetCurrentStateDebug()}");
			}

			if (FeatureFlags.GetLocalConfiguration().DisablePauseBehaviour)
			{
				return;
			}

			if (paused) HandleGamePaused();
			else HandleGameUnpaused();
		}

		private void OnApplicationFocus(ApplicationFocusMessage msg) => OnPause(!msg.IsFocus);

		private void OnApplicationPause(ApplicationPausedMessage msg) => OnPause(msg.IsPaused);
	}
}