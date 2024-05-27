using System;
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
		private MessageBrokerHelper _messageBrokerHelper;

		// events flags
		private bool _gameEnded = false;
		private bool _localPlayerDead = false;

		public QuantumHelper(FLGTestRunner testRunner, MessageBrokerHelper messageBrokerHelper) : base(testRunner)
		{
			_messageBrokerHelper = messageBrokerHelper;
			RunWhenGameAwake(SubscribeToQuantumEvents);
		}

		private void SubscribeToQuantumEvents()
		{
			QuantumEvent.SubscribeManual<EventOnGameEnded>(this, callback =>
			{
				FLGTestRunner.Instance.UseBotBehaviour = false;
				QuantumCallback.UnsubscribeListener<CallbackUpdateView>(this);
				_gameEnded = true;
				_inputManipulator?.Stop();
			});
			QuantumEvent.SubscribeManual<EventOnLocalPlayerDead>(this, callback => { _localPlayerDead = true; });
		}

		public IEnumerator WaitForGameToFinish()
		{
			Debug.Log("WaitForGameToFinish");
			yield return TestTools.Until(() => _gameEnded, 60, "Game did not end!");
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
			yield return WaitForMatchServices();
			_inputManipulator = inputManipulator;
			yield return _inputManipulator.Start();
			RunWhenGameAwake(() =>
			{
				inputManipulator.OnAwake();
				MatchServices.PlayerInputService.OverwriteCallbackInput = _inputManipulator.ChangeInput;
			});
		}

		public IEnumerator UseBotBehaviourForNextMatch()
		{
			FLGTestRunner.Instance.UseBotBehaviour = true;
			yield break;
		}

		public IEnumerator DecreaseCircleTimesForNextMatch()
		{
			QuantumCallback.SubscribeManual<CallbackUpdateView>(this, UpdateViewEvent);
			yield break;
		}

		public void UpdateViewEvent(CallbackUpdateView ev)
		{
			var f = ev.Game.Frames.Predicted;
			var mapRadius = f.Map.WorldSize / FP._2;
			var previousRadius = mapRadius;
			foreach (var quantumShrinkingCircleConfig in f.Context.MapShrinkingCircleConfigs.Values)
			{
				quantumShrinkingCircleConfig.ShrinkingTime = (previousRadius - (previousRadius * quantumShrinkingCircleConfig.ShrinkingSizeK)) / FP._3;
				quantumShrinkingCircleConfig.DelayTime = FP._1;
				quantumShrinkingCircleConfig.WarningTime = FP._5;
				previousRadius *= quantumShrinkingCircleConfig.ShrinkingSizeK;

			}
		}


		public IEnumerator WaitForLocalPlayerToDie()
		{
			Debug.Log("WaitForLocalPlayerToDie");
			yield return TestTools.Until(() => _localPlayerDead, 60, $"Not received local player died!");
		}

		public IEnumerator WaitForSimulationToStart(bool replay = false)
		{
			if (!replay && _inputManipulator != null)
			{
				_inputManipulator.Stop();
			}

			Debug.Log("WaitForSimulationToStart");
			yield return TestTools.Until(IsSimulationRunning, 60, $"Simulation did not start!");
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
				yield return Fail("Simulation is not running yet");
				yield break;
			}

			game.SendCommand(command);
		}
	}
}