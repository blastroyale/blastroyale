#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.TestCases.Helpers;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FirstLight.Game.TestCases
{
	public class TestRunnerBehaviour : MonoBehaviour
	{
	}


	public class FLGTestRunner
	{
		private static FLGTestRunner? _instance;

		private static readonly object Instancelock = new object();

		public static FLGTestRunner Instance
		{
			get
			{
				lock (Instancelock)
				{
					if (_instance == null)
					{
						_instance = new FLGTestRunner();
					}
				}

				return _instance;
			}
		}

		private readonly List<string> _errors = new ();
		private bool _isGameAwaken = false;
		private bool _failCalled = false;
		private PlayTestCase? _runningTest;
		private TestRunnerBehaviour _testRunnerBehaviour = null!;
		private GameObject _runnerGameObject = null!;
		public bool UseBotBehaviour = false;
		public Func<string, IEnumerator>? FailInstruction { private get; set; }
		private readonly FirebaseLab.TestLabManager _testLabManager = FirebaseLab.TestLabManager.Instantiate();
		private TestInstaller? _helpers;

		/// <summary>
		/// This is non blocking, AKA runs in background
		/// </summary>
		/// <param name="testCase"></param>
		private void RunInsideCoroutine(PlayTestCase testCase)
		{
			Setup(testCase);
			_testRunnerBehaviour.StartCoroutine(Run(testCase, false));
		}

		public IEnumerator RunInUnitTest(PlayTestCase testCase)
		{
			Setup(testCase);
			yield return Run(testCase, true);
		}

		private void PublishTestResult(bool success, string message)
		{
			if (_runningTest == null || !_isGameAwaken)
			{
				return;
			}

			MainInstaller.ResolveServices().AnalyticsService.LogEvent("test_result",
				new Dictionary<string, object>()
				{
					{"test_result", success ? "success" : "error"},
					{"test_message", message.Length > 60 ? message.Substring(0, 60) : message},
					{"exceptions", _errors.Count},
				});
		}


		public IEnumerator Fail(string reason)
		{
			if (_failCalled)
			{
				yield break;
			}

			_failCalled = true;
			_testLabManager.AppendResult(false, reason);
			PublishTestResult(false, reason);

			// Idk what happens with metrics if quit instant
			yield return new WaitForSeconds(5);
			if (FailInstruction != null)
			{
				yield return FailInstruction(reason);
			}

			_testLabManager.EarlyQuitApp();
			Debug.LogError("Test failed with reason: " + reason);
		}


		private void Setup(PlayTestCase testCase)
		{
			FLog.Init();
			FLog.Info("Setupping test case" + testCase.GetType().Name);
			_runningTest = testCase;
			_runnerGameObject = new GameObject();
			Object.DontDestroyOnLoad(_runnerGameObject);
			_testRunnerBehaviour = _runnerGameObject.AddComponent<TestRunnerBehaviour>();
			var benchmarkCollector = new BenchmarkCollector(_testLabManager, testCase);
			benchmarkCollector.Start();
			_helpers = SetupHelpers();
			_runningTest.SetInstaller(_helpers);
			Application.logMessageReceived += OnLogReceived;
		}


		private IEnumerator Run(PlayTestCase testCase, bool onTests = true)
		{
			if (onTests)
			{
				// For play mode we need to start the boot scene manually
				yield return LoadBootScene();
			}

			FLog.Info("Calling " + testCase.GetType().Name + ".Run()");

			yield return testCase.Run();
			if (onTests || testCase.IsAutomation)
			{
				// Tests have to leave by themselves
				yield break;
			}

			PublishTestResult(true, _errors.Count > 0 ? "Test finished with exceptions!" : "Success test!");
			_testLabManager.AppendResult(true, _errors.Count > 0 ? "Test finished with exceptions!" : "Success test!");


			MainInstaller.ResolveServices().QuitGame("Success Test");
		}

		private void OnLogReceived(string logMessage, string trace, LogType type)
		{
			if (type is LogType.Error or LogType.Exception)
			{
				_testLabManager.AppendException(logMessage);
				_errors.Add(logMessage);
			}
		}


		private IEnumerator LoadBootScene()
		{
			var loadSceneAsync = SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Single);
			while (!loadSceneAsync.isDone)
			{
				yield return null;
			}
		}

		private TestInstaller SetupHelpers()
		{
			var testInstaller = new TestInstaller();
			var messageBrokerHelper = new MessageBrokerHelper(this);
			testInstaller.Bind(messageBrokerHelper);
			testInstaller.Bind(new QuantumHelper(this, messageBrokerHelper));
			testInstaller.Bind(new AccountHelper(this));
			testInstaller.Bind(new FeatureFlagsHelper(this));
			testInstaller.Bind(new PlayerConfigsHelper(this));
			var uiHelper = new UIHelper(this);
			testInstaller.Bind(uiHelper);
			testInstaller.Bind(new BattlePassUIHelper(this, uiHelper));
			testInstaller.Bind(new GameUIHelper(this, uiHelper));
			testInstaller.Bind(new HomeUIHelper(this, uiHelper));
			testInstaller.Bind(new GamemodeUIHelper(this, uiHelper));
			testInstaller.Bind(new GameConfigHelper(this));
			return testInstaller;
		}


		public void CheckFirebaseRun()
		{
			if (_testLabManager.IsTestingScenario)
			{
				FLog.Info("Detected case scenario: " + _testLabManager.ScenarioNumber);
				PlayTestCase testCase = _testLabManager.ScenarioNumber switch
				{
					1 => new TenMatchesInARow(),
					2 => new TwoMatchesInARow(),
					_ => new TutorialTestCase()
				};
				RunInsideCoroutine(testCase);
			}

			if (_runningTest == null) return;

			_runningTest.BeforeGameAwaken();
		}

		public bool IsRunning()
		{
			return _runningTest != null;
		}

		public string? GetRunningTestName()
		{
			return _runningTest?.GetType().Name;
		}

		public void CheckAutomations()
		{
#if UNITY_EDITOR
			if (FeatureFlags.GetLocalConfiguration().StartTestGameAutomatically)
			{
				RunInsideCoroutine(new JoinTestRoom());
			}
#endif
		}

		public void BeforeGameAwaken()
		{
			if (_runningTest == null) return;

			_runningTest.BeforeGameAwaken();
		}

		public void AfterGameAwaken()
		{
			if (_runningTest == null) return;
			_helpers?.OnGameAwaken();
			_runningTest.AfterGameAwaken();
			_isGameAwaken = true;
			MainInstaller.ResolveServices().AnalyticsService.LogEvent("test_started",
				new Dictionary<string, object>()
				{
					{"name", _runningTest.GetType().Name},
				});
		}
	}
}