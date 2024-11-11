using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.StateMachines;
using FirstLight.Game.TestCases;
using FirstLight.Game.Utils;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Handles game application proccess
	/// </summary>
	public interface IGameAppService
	{
		/// <summary>
		/// Application data for runtime. Will contain a range of information from
		/// version locks and feature flags.
		///
		/// This is basically the game "input" to run at the moment.
		/// </summary>
		IReadOnlyDictionary<string, string> AppData { get; }
		
		/// <summary>
		/// Gets device performance data
		/// </summary>
		PerformanceManager PerformanceManager { get; }
	}

	public class GameAppService : IGameAppService
	{
		private IGameServices _services;
		private IReadOnlyDictionary<string, string> _appData;
		private static readonly TimeSpan _maxPauseTime = TimeSpan.FromMinutes(5);
		private DateTime _pauseTime;
		private bool _paused;
		public PerformanceManager PerformanceManager { get; private set; }

		public GameAppService(IGameServices services)
		{
			_services = services;
			_services.MessageBrokerService.Subscribe<FeatureFlagsReceived>(OnFeatureFlags);
			PerformanceManager = new PerformanceManager();
		}

		private void OnFeatureFlags(FeatureFlagsReceived e)
		{
			_appData = e.AppData;
			Application.runInBackground = !FeatureFlags.GetLocalConfiguration().DisableRunInBackground;
			PerformanceManager.Initialize(e.AppData);
			if (!FeatureFlags.PAUSE_DISCONNECT_DIALOG)
			{
				FLog.Verbose("Pause behaviour disabled");
				return;
			}
			
			FLog.Verbose("Pause behaviour enabled");
			_services.MessageBrokerService.Subscribe<ApplicationFocusMessage>(OnApplicationFocus);
			_services.MessageBrokerService.Subscribe<ApplicationPausedMessage>(OnApplicationPause);
		}

		private void HandleGamePaused()
		{
			if (!_services.AuthenticationService.State.LoggedIn) return;
			if (_paused) return;
			_paused = true;
			_pauseTime = DateTime.UtcNow;
			FLog.Info("Game Paused");
		}

		private void HandleGameUnpaused()
		{
			if (!_services.AuthenticationService.State.LoggedIn || _services.NetworkService.QuantumClient == null) return;
			if (!Application.isPlaying) return;
#if UNITY_EDITOR
			if (UnityEditor.EditorApplication.isPaused) return;
#endif
			if (!_paused) return;
			if (DateTime.UtcNow - _pauseTime > _maxPauseTime)
			{
				FLog.Warn("Max pause time elapsed");
				_services.GenericDialogService.OpenSimpleMessage("Disconnected", "Please Restart", Application.Quit);
				return;
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

			if (FeatureFlags.GetLocalConfiguration().DisableRunInBackground)
			{
				return;
			}

			if (paused) HandleGamePaused();
			else HandleGameUnpaused();
		}

		private void OnApplicationFocus(ApplicationFocusMessage msg) => OnPause(!msg.IsFocus);

		private void OnApplicationPause(ApplicationPausedMessage msg) => OnPause(msg.IsPaused);
		public IReadOnlyDictionary<string, string> AppData => _appData;
	}
}