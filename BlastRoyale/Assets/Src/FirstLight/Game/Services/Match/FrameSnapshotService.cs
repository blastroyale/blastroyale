using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Photon.Deterministic;
using Quantum;
using UnityEngine.Serialization;
using BitStream = Photon.Deterministic.BitStream;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service provides the possibility of storing and retrieving match snapshots, which can be used to start 
	/// a quantum game from certain points.
	/// A snapshot is a serialized data of a MatchSetup and a Frame of the game, which can be restored by either
	/// simply using the match setup to reconnect to the game or re-create the game based that given certain point.
	///
	/// Re creation of the game does not work on live environment except for custom or offline games as that would be
	/// an exploitable way for easy rewards
	/// </summary>
	public interface IFrameSnapshotService
	{
		/// <summary>
		/// Takes a local snapshot of the current game state.
		/// Saves the current snapshot (RoomSetup + Frame Data) in AppData
		/// </summary>
		void TakeSnapshot(QuantumGame game);

		/// <summary>
		/// Clears last frame snapshot from AppData.
		/// </summary>
		void ClearFrameSnapshot();
	}

	/// <summary>
	/// Stores data of a frame captured from a quantum game game
	/// </summary>
	[Serializable]
	public struct FrameSnapshot
	{
		public byte AmtPlayers;
		public bool Offline;
		public long ExpiresAt;
		public string RoomName;
		public byte[] SnapshotBytes;
		public int FrameNumber;
		public byte[] SerializedSetup;

		public override string ToString()
		{
			return $"<Snapshot Frame={FrameNumber} Setup={SerializedSetup} Room={RoomName} Offline={Offline} QtdPlayers={AmtPlayers}>";
		}

		/// <summary>
		/// Checks if the given snapshot can still be re-used. Even if failed when trying it should be ok
		/// We just check for expiry to save the hassle.
		/// </summary>
		public bool Expired()
		{
			return AmtPlayers == 0 || DateTime.UtcNow.Ticks > ExpiresAt;
		}
	}

	public class FrameSnapshotService : IFrameSnapshotService, MatchServices.IMatchService
	{
		private readonly IDataService _dataService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _data;

		private readonly CancellationTokenSource _disposeCancelation = new CancellationTokenSource();

		public FrameSnapshotService(IDataService dataService)
		{
			_data = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_dataService = dataService;
			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			_services.MessageBrokerService.Subscribe<SimulationEndedMessage>(OnSimulationEnd);
		}

		private void OnSimulationEnd(SimulationEndedMessage msg)
		{
			if (msg.Reason == SimulationEndReason.Disconnected && msg.Game != null)
			{
				TakeSnapshot(msg.Game);
			}
			else
			{
				ClearFrameSnapshot();
			}
		}

		private void OnMatchStarted(MatchStartedMessage msg)
		{
			if (QuantumRunner.Default != null && QuantumRunner.Default.Game?.Frames.Verified != null)
			{
				SnapshotRoutine(_disposeCancelation.Token).Forget();
			}
		}

		private unsafe bool CanGameBeReconnected()
		{
			if (QuantumRunner.Default == null || !QuantumRunner.Default.IsDefinedAndRunning(false)) return false;
			if (!QuantumRunner.Default.Game.Frames.Verified.Unsafe.TryGetPointerSingleton<GameContainer>(out var container)) return false;
			if (container->IsGameOver) return false;
			return true;
		}

		public void TakeSnapshot(QuantumGame game)
		{
			//TODO: Reconnection broken don't let me merge
			if (!CanGameBeReconnected()) return;

			var lastRoom = _services.RoomService.LastRoom;
			var timeToEndGame = TimeSpan.FromSeconds(GameConstants.Network.TIMEOUT_SNAPSHOT_SECONDS);
			var snapshot = new FrameSnapshot
			{
				RoomName = lastRoom?.Name,
				ExpiresAt = DateTime.UtcNow.Ticks + timeToEndGame.Ticks,
				Offline = lastRoom?.IsOffline ?? false,
				SerializedSetup = lastRoom?.ToMatchSetup().ToByteArray(),
				AmtPlayers = (byte) lastRoom?.GetRealPlayerAmount(),
			};

			if (snapshot.Offline)
			{
				snapshot.SnapshotBytes = game.Frames.Verified.Serialize(DeterministicFrameSerializeMode.Blit);
				snapshot.FrameNumber = game.Frames.Verified.Number;
			}
			_data.AppDataProvider.LastFrameSnapshot.Value = snapshot;
			_services.DataSaver.SaveData<AppData>();
			FLog.Info($"Frame snapshot captured {snapshot}");
		}

		public void ClearFrameSnapshot()
		{
			if (_data.AppDataProvider.LastFrameSnapshot != default)
			{
				FLog.Info("Clearing Frame Snapshot");
				_data.AppDataProvider.LastFrameSnapshot.Value = default;
				_services.DataSaver.SaveData<AppData>();
			}
		}

		public async UniTaskVoid SnapshotRoutine(CancellationToken ctsToken)
		{
			while (QuantumRunner.Default.IsDefinedAndRunning(false) && !ctsToken.IsCancellationRequested)
			{
				TakeSnapshot(QuantumRunner.Default.Game);
				await UniTask.WaitForSeconds(30, cancellationToken: ctsToken);
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_disposeCancelation.Cancel();
			_disposeCancelation.Dispose();
			QuantumCallback.UnsubscribeListener(this);
			_services.MessageBrokerService.UnsubscribeAll(this);
		}

		/// <inheritdoc />
		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			// Do Nothing
		}

		/// <inheritdoc />
		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
			// Do Nothing
		}
	}
}