using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Services;
using Newtonsoft.Json;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service provides the possibility of storing and retrieving match snapshots, which can be used to start 
	/// a quantum game from certain points.
	/// </summary>
	public interface IFrameSnapshotService
	{
		/// <summary>
		/// Returns the last captured snapshot from when the last quantum game that was destroyed
		/// </summary>
		FrameSnapshot GetLastStoredMatchSnapshot();
	}

	/// <summary>
	/// Stores data of a frame captured from a quatnum game
	/// </summary>
	[Serializable]
	public struct FrameSnapshot
	{
		public byte[] SnapshotBytes;
		public int SnapshotNumber;
	}

	public class FrameSnapshotService : IFrameSnapshotService, MatchServices.IMatchService
	{
		private FrameSnapshot _lastCapturedSnapshot;
		private readonly IDataService _dataService;

		public FrameSnapshotService(IDataService dataService)
		{
			_dataService = dataService;
			_lastCapturedSnapshot = _dataService.GetData<AppData>().LastCapturedFrameSnapshot;

			QuantumCallback.SubscribeManual<CallbackGameDestroyed>(this, OnQuantumGameDestroyed);
		}

		/// <inheritdoc />
		public FrameSnapshot GetLastStoredMatchSnapshot()
		{
			return _lastCapturedSnapshot;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_dataService.GetData<AppData>().LastCapturedFrameSnapshot = _lastCapturedSnapshot;
			_dataService.SaveData<AppData>();
			QuantumCallback.UnsubscribeListener(this);
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

		private void OnQuantumGameDestroyed(CallbackGameDestroyed callback)
		{
			_lastCapturedSnapshot = new FrameSnapshot()
			{
				SnapshotBytes = callback.Game.Frames.Verified.Serialize(DeterministicFrameSerializeMode.Blit),
				SnapshotNumber = callback.Game.Frames.Verified.Number
			};
		}
	}
}