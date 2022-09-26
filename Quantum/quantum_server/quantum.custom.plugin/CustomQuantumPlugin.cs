using FirstLight.Game.Commands;
using Photon.Deterministic;
using Photon.Deterministic.Server.Interface;
using Photon.Hive.Plugin;
using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Ids;

namespace Quantum
{
	/// <summary>
	/// Class represents a quantum plugin. It wraps around a server.
	/// This instance is created by PluginFactory everytime a match starts.
	/// </summary>
	public class CustomQuantumPlugin : DeterministicPlugin
	{
		public readonly CustomQuantumServer CustomServer;
		private MatchType _matchType = MatchType.Custom;
		private QuantumCommandHandler _cmdHandler;

		public CustomQuantumPlugin(Dictionary<String, String> config, IServer server) : base(server)
		{
			Assert.Check(server is CustomQuantumServer);
			CustomServer = (CustomQuantumServer)server;
			_cmdHandler = new QuantumCommandHandler(this);
		}

		/// <summary>
		/// Called whenever any client sends an event
		/// </summary>
		public override void OnRaiseEvent(IRaiseEventCallInfo info)
		{
			if (info.Request.EvCode == (int)QuantumCustomEvents.EndGameCommand)
			{
				info.Cancel();
				var client = CustomServer.GetClientForActor(info.ActorNr);
				if (client == null)
				{
					return;
				}
				if (info.Request.Data == null)
				{
					CustomServer.DisconnectClient(client, "Invalid command data");
					return;
				}
				var bytes = (byte[])info.Request.Data;
				_cmdHandler.ReceiveEndGameCommand(info.ActorNr, bytes);
				return;
			}
			base.OnRaiseEvent(info);
		}

		/// <summary>
		/// Called when a player joins a room. Is not called for the first player (as he creates it)
		/// </summary>
		public override void OnJoin(IJoinGameCallInfo info)
		{
			base.OnJoin(info);
			Log.Info($"Actor {info.Request.ActorNr} joined with userId {info.UserId}");
		}

		/// <summary>
		/// Called when a player creates a game.
		/// </summary>
		public override void OnCreateGame(ICreateGameCallInfo info)
		{
			Log.Info($"Actor {info.Request.ActorNr} created & joined with userId {info.UserId}");
			base.OnCreateGame(info);
			if (!info.CreateOptions.TryGetValue("CustomProperties", out var propsObject))
			{
				Log.Debug("No Custom Properties");
				return;
			}
			var customProperties = propsObject as Hashtable;
			if (customProperties == null)
			{
				Log.Debug("No Custom Properties");
				return;
			}
			Enum.TryParse((string)customProperties["matchType"], out _matchType);
			Log.Info($"Created {_matchType.ToString()} game");
		}

		/// <summary>
		/// Called whenever a player leaves the room. 
		/// Will be called for every player before EndGame is called.
		/// </summary>
		public override void OnLeave(ILeaveGameCallInfo info)
		{
			base.OnLeave(info);
		}

		/// <summary>
		/// Handler for after a game is closed.
		/// All players likely will already have left the room when the game is closed.
		/// This is the last step before the plugin is disposed.
		/// </summary>
		public override void OnCloseGame(ICloseGameCallInfo info)
		{
			_cmdHandler.DispatchAllCommands();
			base.OnCloseGame(info);
		}

		public MatchType GetMatchType() => _matchType;
	}
}
