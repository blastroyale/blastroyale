using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLight.Services;
using Photon.Realtime;
using UnityEngine;

namespace FirstLight.Game.Services
{
	public interface IServerListService
	{
		public void FetchServersAndPing();
		public IReadOnlyDictionary<string, ServerPing> PingsByServer { get; }
		public ServerListState State { get; }

		public event Action OnRegionsPinged;
		public event Action OnRegionsFetched;

		public enum ServerListState
		{
			Uninitialized,
			Connecting,
			FetchedRegions,
			FetchedPings,
			Failed,
		}

		public class ServerPing
		{
			public string ServerCode { get; }
			public bool ReceivedPing { get; }
			public string ServerIp { get; }
			public long Ping { get; }

			public ServerPing(string serverCode, bool receivedPing, long ping, string serverIp)
			{
				ServerCode = serverCode;
				ReceivedPing = receivedPing;
				Ping = ping;
				ServerIp = serverIp;
			}
		}
	}


	public class ServerListService : IServerListService, IConnectionCallbacks
	{
		private IThreadService _threadService;
		private ICoroutineService _coroutineService;
		private readonly IGameBackendService _backendService;
		private LoadBalancingClient _client;
		private Dictionary<string, IServerListService.ServerPing> Pings = new ();

		private bool _fetching;

		public ServerListService(IThreadService threadService, ICoroutineService coroutineService, IGameBackendService backendService, IMessageBrokerService _messageBrokerService)
		{
			_threadService = threadService;
			_coroutineService = coroutineService;
			_backendService = backendService;
			_messageBrokerService.Subscribe<SuccessAuthentication>(OnAuthentication);
		}

		private void OnAuthentication(SuccessAuthentication obj)
		{
			FetchServersAndPing();
		}

		public IEnumerator TickQuantum()
		{
			while (_fetching)
			{
				yield return new WaitForSeconds(1);
				_client?.Service();
			}
		}


		public IReadOnlyDictionary<string, IServerListService.ServerPing> PingsByServer => Pings;
		public IServerListService.ServerListState State { get; private set; }


		public event Action OnRegionsPinged;
		public event Action OnRegionsFetched;


		public void OnConnected()
		{
			FLog.Info("OnConnected");
		}

		public void OnConnectedToMaster()
		{
		}

		public void OnDisconnected(DisconnectCause cause)
		{
			FLog.Info("Disconnected" + cause);
			Disconnect();
		}

		public void FetchServersAndPing()
		{
			_client = new LoadBalancingClient("", FLEnvironment.Current.PhotonAppIDRealtime, "");
			_client.AddCallbackTarget(this);
			if (_fetching)
			{
				FLog.Info("Already collecting regions and pings!");
				return;
			}

			FLog.Info("Fetching pings");
			if (!_client.ConnectToNameServer())
			{
				State = IServerListService.ServerListState.Failed;
				FLog.Error("Failed to connect to name server");
			}

			_fetching = true;
			State = IServerListService.ServerListState.Connecting;
			_coroutineService.StartCoroutine(TickQuantum());
		}

		public void OnRegionListReceived(RegionHandler regionHandler)
		{
			FLog.Info("OnPingedRegions" + regionHandler.GetResults());
			UpdateList(regionHandler);
			State = IServerListService.ServerListState.FetchedRegions;
			FLog.Info("Fetched Regions");

			if (OnRegionsFetched != null)
				_threadService.MainThreadDispatcher.Enqueue(OnRegionsFetched);
			OnRegionsFetched?.Invoke();
			if (!regionHandler.PingMinimumOfRegions(OnFetchedRegionPings, ""))
			{
				State = IServerListService.ServerListState.Failed;
				FLog.Error("Failed to fetch pings");
				Disconnect();
			}
		}

		private void Disconnect()
		{
			_client?.RemoveCallbackTarget(this);
			_client?.Disconnect();
			_fetching = false;
		}

		private void OnFetchedRegionPings(RegionHandler obj)
		{
			FLog.Info("Fetched pings");
			UpdateList(obj);
			if (OnRegionsPinged != null)
				_threadService.MainThreadDispatcher.Enqueue(OnRegionsPinged);
			State = IServerListService.ServerListState.FetchedPings;
			Disconnect();
		}

		private void UpdateList(RegionHandler obj)
		{
			foreach (var objEnabledRegion in obj.EnabledRegions)
			{
				Pings[objEnabledRegion.Code] = new IServerListService.ServerPing(objEnabledRegion.Code, objEnabledRegion.WasPinged, objEnabledRegion.Ping, objEnabledRegion.HostAndPort);
			}
		}


		public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
		{
		}

		public void OnCustomAuthenticationFailed(string debugMessage)
		{
		}
	}
}