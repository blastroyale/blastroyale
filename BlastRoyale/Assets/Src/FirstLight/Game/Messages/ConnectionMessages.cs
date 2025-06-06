using System.Net;
using Photon.Realtime;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	/// <summary>
	/// Message fired when requests to server fails for some reason.
	/// </summary>
	public struct ServerHttpErrorMessage : IMessage
	{
		public HttpStatusCode ErrorCode;
		public string Message;
	}
	
	public struct PingedRegionsMessage : IMessage { public RegionHandler RegionHandler; }

	public struct ChangedServerRegionMessage : IMessage
	{
	}
	
	public struct NetworkActionWhileDisconnectedMessage : IMessage { }
	public struct AttemptManualReconnectionMessage : IMessage { }
}