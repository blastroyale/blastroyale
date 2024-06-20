using System.Collections;
using FirstLight.Game.Configs;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.TestCases.Helpers
{
	public class ReplayerManipulator : IInputManipulator
	{
		private string _replayFileName;

		private int _replayIndex;
		private int _replayFirstFrame;
		private ReplayFile _replayFile;

		public ReplayerManipulator(string replayFileName)
		{
			_replayFileName = replayFileName;
		}

		public IEnumerator LoadFile()
		{
			var serializer = new QuantumUnityJsonSerializer();
			var loadTask = Addressables.LoadAssetAsync<TextAsset>($"Assets/AddressableResources/Replays/Inputs/{_replayFileName}.json");

			yield return new WaitUntil(() => loadTask.IsDone);
			if (loadTask.OperationException != null)
			{
				throw loadTask.OperationException;
			}


			_replayFile = serializer.DeserializeReplay(loadTask.Result.bytes);
			_replayFirstFrame = _replayFile.InputHistory[0].Inputs[0].Tick;
		}


		public void ChangeInput(CallbackPollInput callback, ref Quantum.Input input)
		{
			if (_replayFile == null)
			{
				return;
			}

			if (_replayFirstFrame > callback.Frame)
			{
				return;
			}

			// Commands in this frame only will be executed in the next one, so i need to process them one frame before
			if (_replayIndex + 1 < _replayFile.InputHistory.Length)
			{
				HandleCommands(_replayFile.InputHistory[_replayIndex + 1].Inputs[0]);
			}

			if (_replayIndex >= _replayFile.InputHistory.Length)
			{
				//ReplayMenu.ExportDialogReplayAndDB(QuantumRunner.Default.Game, new QuantumUnityJsonSerializer(), ".json");
				_replayFile = null;
				return;
			}

			var replayInput = _replayFile.InputHistory[_replayIndex].Inputs[0];
			var replayInputData = replayInput.DataArray;
			if (replayInput.DataArray.Length == 3)
			{
				input.B1 = replayInputData[0];
				input.B2 = replayInputData[1];
				input.B3 = replayInputData[2];
			}

			_replayIndex++;
		}

		private void HandleCommands(DeterministicTickInput inputTick)
		{
			if (inputTick.Rpc.Length == 0)
			{
				return;
			}

			if ((inputTick.Flags & DeterministicInputFlags.Command) != DeterministicInputFlags.Command)
			{
				// This is set player data
				return;
			}

			var stream = new Photon.Deterministic.BitStream(inputTick.Rpc);
			var serializer = QuantumRunner.Default.Game.Session.CommandSerializer;
			serializer.ReadNext(stream, out var commandInstance);
			QuantumRunner.Default.Game.SendCommand(commandInstance);
		}


		public IEnumerator Start()
		{
			yield return LoadFile();
		}


		public void OnAwake()
		{
		}

		void IInputManipulator.Stop()
		{
			_replayFile = null;
		}
	}
}