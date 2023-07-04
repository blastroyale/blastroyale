using System;
using System.Collections;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.TestCases.Helpers
{
	public class TestHelper
	{
		private FLGTestRunner _testRunner;
		private Action _gameAwakeRun;
		private Action _authenticatedRun;
		private bool _isGameAwake = false;
		private bool _authenticated = false;

		public TestHelper(FLGTestRunner testRunner)
		{
			_testRunner = testRunner;
			RunWhenGameAwake(SetupTestHelperBase);
		}

		private void SetupTestHelperBase()
		{
			Services.MessageBrokerService.Subscribe<SuccessAuthentication>(authentication =>
			{
				_authenticated = true;
				_authenticatedRun?.Invoke();
				_authenticatedRun = null;
			});
		}

		public void OnGameAwaken()
		{
			_isGameAwake = true;
			_gameAwakeRun?.Invoke();
		}

		public void RunWhenGameAwake(Action ac)
		{
			if (_isGameAwake)
			{
				ac.Invoke();
				return;
			}

			_gameAwakeRun += ac;
		}

		public void RunWhenAuthenticated(Action ac)
		{
			if (_authenticated)
			{
				ac.Invoke();
				return;
			}

			_authenticatedRun += ac;
		}

		protected IGameServices Services => MainInstaller.ResolveServices();
		protected IGameDataProvider DataProviders => MainInstaller.Resolve<IGameDataProvider>();
		protected IMatchServices MatchServices => MainInstaller.ResolveMatchServices();


		public IEnumerator WaitForMatchServices()
		{
			return TestTools.Until(() => MainInstaller.TryResolve<IMatchServices>(out _), 120, "Match services did not start");
		}

		public IEnumerator Fail(String reason)
		{
			yield return _testRunner.Fail(reason);
		}


		public void Log(string str)
		{
			Debug.Log("[Tests] " + str);
		}

		public IEnumerator WaitForGameAwaken()
		{
			return TestTools.Until(() => _isGameAwake, 60, "Game did not awake!");
		}
	}
}