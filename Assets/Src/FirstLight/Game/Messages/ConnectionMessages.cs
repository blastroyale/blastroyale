using System.Collections.Generic;
using System.Net;
using ExitGames.Client.Photon;
using FirstLight.Services;
using Photon.Realtime;

namespace FirstLight.Game.Messages
{
	public struct PlayerLoadedMatchMessage : IMessage { public Player Player; }
	
	/// <summary>
	/// Message fired when requests to server fails for some reason.
	/// </summary>
	public struct ServerHttpErrorMessage : IMessage
	{
		public HttpStatusCode ErrorCode;
		public string Message;
	}
	
	public struct PingedRegionsMessage : IMessage
	{
		public RegionHandler RegionHandler;
	}
	
	public struct RegionListReceivedMessage : IMessage
	{
		public RegionHandler RegionHandler;
	}
}