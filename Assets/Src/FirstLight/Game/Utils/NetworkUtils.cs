using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
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
				FLog.Warn("Client is offline, sending network disconnected message");
				MainInstaller.Resolve<IGameServices>().MessageBrokerService.Publish(new NetworkActionWhileDisconnectedMessage());
				return false;
			}

			return true;
		}
	}
}