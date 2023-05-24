using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.StateMachines;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace FirstLight.Game.TestCases.Helpers
{
	public class QuantumHelper : TestHelper
	{
		private IInputManipulator _inputManipulator;

		// events flags
		private bool _gameEnded = false;
		private bool _localPlayerDead = false;

		public QuantumHelper(FLGTestRunner testRunner) : base(testRunner)
		{
			RunWhenGameAwake(SubscribeToQuantumEvents);
		}

		private void SubscribeToQuantumEvents()
		{
			QuantumEvent.SubscribeManual<EventOnGameEnded>(this, callback =>
			{
				_gameEnded = true;
				_inputManipulator?.Stop();
			});
			QuantumEvent.SubscribeManual<EventOnLocalPlayerDead>(this, callback => { _localPlayerDead = true; });
			Services.GameUiService.ScreenStartOpening += OnScreenOpened;
		}

		private void OnScreenOpened(string type)
		{
			if (typeof(MatchEndScreenPresenter).ToString() == type)
			{
				// This is the exact moment the user loses the input hability, when the match end screen open
				if (_inputManipulator != null)
				{
					MatchControlsHudPresenter.OverwriteCallbackInput = null;
					_inputManipulator.Stop();
				}
			}
		}


		public IEnumerator WaitForGameToFinish()
		{
			Debug.Log("WaitForGameToFinish");
			yield return TestTools.Until(() => _gameEnded, 60, true);
		}

		public IEnumerator WaitForSimulationAndRunReplay(string replayName)
		{
			yield return SetInputManipulator(new ReplayerManipulator(replayName));
			yield return WaitForSimulationToStart(true);
		}

		public IEnumerator StopInputManipulator()
		{
			_inputManipulator.Stop();
			_inputManipulator = null;
			yield break;
		}

		public IEnumerator SetInputManipulator(IInputManipulator inputManipulator)
		{
			_inputManipulator = inputManipulator;
			yield return _inputManipulator.Start();
			RunWhenGameAwake(() =>
			{
				inputManipulator.OnAwake();
				MatchControlsHudPresenter.OverwriteCallbackInput = _inputManipulator.ChangeInput;
			});
		}

		public IEnumerator WaitForLocalPlayerToDie()
		{
			Debug.Log("WaitForLocalPlayerToDie");
			yield return TestTools.Until(() => _localPlayerDead, 60, true);
		}

		public IEnumerator WaitForSimulationToStart(bool replay = false)
		{
			if (!replay && _inputManipulator != null)
			{
				_inputManipulator.Stop();
			}

			Debug.Log("WaitForSimulationToStart");
			yield return TestTools.Until(IsSimulationRunning, 60, true);
		}


		private bool IsSimulationRunning()
		{
			return QuantumRunner.Default != null && QuantumRunner.Default.IsRunning;
		}

		public IEnumerator SendCommand(DeterministicCommand command)
		{
			var game = QuantumRunner.Default.Game;
			if (game == null)
			{
				Fail("Simulation is not running yet");
				yield break;
			}

			game.SendCommand(command);
		}
	}
}