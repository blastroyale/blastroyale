using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Services.RoomService;
using FirstLight.Server.SDK.Modules;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using Environment = FirstLight.Game.Services.Environment;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This utility class provides functionality for getting data used in photon network operations
	/// </summary>
	public static class NetworkUtils
	{
	

		/// <summary>
		/// Returns the current map in rotation, used for creating rooms with maps in rotation
		/// </summary>
		public static QuantumMapConfig GetRotationMapConfig(string gameModeId, IGameServices services)
		{
			var gameModeConfig = services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId);
			var compatibleMaps = new List<QuantumMapConfig>();
			var span = DateTime.UtcNow - DateTime.UtcNow.Date;
			var timeSegmentIndex =
				Mathf.RoundToInt((float) span.TotalMinutes / GameConstants.Balance.MAP_ROTATION_TIME_MINUTES);

			foreach (var mapId in gameModeConfig.AllowedMaps)
			{
				var mapConfig = services.ConfigsProvider.GetConfig<QuantumMapConfig>((int) mapId);
				if (!mapConfig.IsTestMap && !mapConfig.IsCustomOnly)
				{
					compatibleMaps.Add(mapConfig);
				}
			}

			if (timeSegmentIndex >= compatibleMaps.Count)
			{
				timeSegmentIndex %= compatibleMaps.Count;
			}

			return compatibleMaps[timeSegmentIndex];
		}
        
	


		/// <summary>
		/// Requests to check if the device is online
		/// </summary>
		public static bool IsOnline()
		{
			return Application.internetReachability != NetworkReachability.NotReachable;
		}

		/// <summary>
		/// Requests to check if the device is offline
		/// </summary>
		public static bool IsOffline()
		{
			return Application.internetReachability == NetworkReachability.NotReachable;
		}

		/// <summary>
		/// Requests to check if the device is connected to internet, and Photon is connected
		/// </summary>
		public static bool IsOnlineAndConnected()
		{
			return IsOnline() && MainInstaller.Resolve<IGameServices>().NetworkService.QuantumClient.IsConnected;
		}

		/// <summary>
		/// Requests to check if the device is disconnted from internet, or Photon is disconnected
		/// </summary>
		public static bool IsOfflineOrDisconnected()
		{
			return IsOffline() || !MainInstaller.Resolve<IGameServices>().NetworkService.QuantumClient.IsConnected;
		}

		/// <summary>
		/// Checks to see if a network action triggered by player input can be sent.
		/// Sends a NetworkActionWhileDisconnectedMessage if not.
		/// </summary>
		public static bool CheckAttemptNetworkAction()
		{
			if (IsOfflineOrDisconnected())
			{
				MainInstaller.Resolve<IGameServices>().MessageBrokerService.Publish(new NetworkActionWhileDisconnectedMessage());
				return false;
			}

			return true;
		}

		/// <summary>
		/// Returns a random dropzone vector to be added to room creation params
		/// </summary>
		public static Vector3 GetRandomDropzonePosRot()
		{
			var radiusPosPercent = GameConstants.Balance.MAP_DROPZONE_POS_RADIUS_PERCENT;
			return new Vector3(Random.Range(-radiusPosPercent, radiusPosPercent),
				Random.Range(-radiusPosPercent, radiusPosPercent), Random.Range(0, 360));
		}
        
	}
}