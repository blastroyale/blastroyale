using System.Linq;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Newtonsoft.Json;
using Photon.Realtime;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	public static class NetworkExtensions
	{
        
		public static string GetRoomDebugString(this Room room)
		{
			var s = MainInstaller.Resolve<IGameServices>();
			return JsonConvert.SerializeObject(new
			{
				LastDisconnectionLocation = s.NetworkService.LastDisconnectLocation,
				JoinSource = s.NetworkService.JoinSource.ToString(),
				Players = room.Players.Values.Select(p => new
				{
					p.UserId, p.NickName, Inactive=p.IsInactive, Master=p.IsMasterClient, Props=p.CustomProperties.ToString()
				}),
				ExpectedUsers = room.ExpectedUsers,
				LoadingStartsAt = s.RoomService.CurrentRoom.Properties.LoadingStartServerTime.Value,
				PlayereTTL = room.PlayerTtl,
				EmptyRoomTTL = room.EmptyRoomTtl,
				MaxPlayers=room.MaxPlayers,	
				Props=room.CustomProperties.ToString(),
				PlayerCount=room.PlayerCount
			}, Formatting.Indented);
		}

		public static string GetSimulationDebugString(this QuantumRunner runner)
		{
			if (runner == null)
			{
				Debug.Log("NULL");
			}
			
			var s = MainInstaller.Resolve<IGameServices>();
			return JsonConvert.SerializeObject(new
			{
				LocalPlayerActive = !s.NetworkService.LocalPlayer.IsInactive,
				HasGameTimedOut = runner.HasGameStartTimedOut,
				LocalPlayerRef = runner.Game.GetLocalPlayerRef(),
				Running = runner.IsRunning,
				Stall = runner.Session.IsStalling,
				Pause = runner.Session.IsPaused,
				Online = runner.Session.IsOnline,
				Spectating = runner.Session.IsSpectating,
				Active = runner.isActiveAndEnabled,
				LocalPlayers = runner.Session.LocalPlayerIndices.Length,
				Predicted = runner.Session.IsPredicted,
				Stats = runner.Session.Stats,
				LocalInputOffset = runner.Session.LocalInputOffset,
				IsReplayFinished = runner.Session.IsReplayFinished
			}, Formatting.Indented);
		}

		public static bool IsDefinedAndRunning(this QuantumRunner runner, bool considerStalling = true)
		{
			return runner != null && !runner.IsDestroyed() && runner.isActiveAndEnabled && runner.IsRunning && (!considerStalling || !runner.Session.IsStalling) &&
				!runner.Session.IsPaused;
		}

		public static Frame PredictedFrame(this QuantumRunner runner)
		{
			return runner.Game.Frames.Predicted;
		}
		
		public static Frame VerifiedFrame(this QuantumRunner runner)
		{
			return runner.Game.Frames.Verified;
		}
		
	}
}