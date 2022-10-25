using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Services;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

/// <summary>
/// This service provides the possibility of storing and retrieving match snapshots, which can be used to start 
/// a quantum game from certain points.
/// </summary>
public interface IMatchFrameSnapshotService
{
	/// <summary>
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

public class FrameSnapshotService : IMatchFrameSnapshotService, MatchServices.IMatchService
{
	private FrameSnapshot _lastCapturedSnapshot;
	private readonly IDataService _dataService;
	
	public FrameSnapshotService(IDataService dataService)
	{
		_dataService = dataService;
		_lastCapturedSnapshot = _dataService.GetData<AppData>().LastCapturedFrameSnapshot;

		QuantumCallback.SubscribeManual<CallbackUpdateView>(this, OnQuantumUpdateView);
	}

	private void OnQuantumUpdateView(CallbackUpdateView callback)
	{
		_lastCapturedSnapshot = new FrameSnapshot()
		{
			SnapshotBytes = callback.Game.Frames.Verified.Serialize(DeterministicFrameSerializeMode.Blit),
			SnapshotNumber = callback.Game.Frames.Verified.Number
		};
	}
	
	public FrameSnapshot GetLastStoredMatchSnapshot()
	{
		return _lastCapturedSnapshot;
	}

	public void Dispose()
	{
		_dataService.GetData<AppData>().LastCapturedFrameSnapshot = _lastCapturedSnapshot;
		_dataService.SaveData<AppData>();
		QuantumCallback.UnsubscribeListener(this);
	}

	public void OnMatchStarted(QuantumGame game, bool isReconnect)
	{
	}

	public void OnMatchEnded()
	{
	}
}
