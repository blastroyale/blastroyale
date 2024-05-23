using FirstLight.Game.Commands;
using Photon.Deterministic;
using Photon.Deterministic.Server.Interface;
using Photon.Hive.Plugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Ids;
using quantum.custom.plugin;
using System.Text;
using FirstLight.Game.Services.RoomService;

namespace Quantum
{
	/// <summary>
	/// Class represents a quantum plugin. It wraps around a server.
	/// This instance is created by PluginFactory everytime a match starts.
	/// </summary>
	public class CustomQuantumPlugin : DeterministicPlugin
	{
		public readonly CustomQuantumServer CustomServer;
		private RoomProperties _roomProps;
		private QuantumCommandHandler _cmdHandler;

		public CustomQuantumPlugin(Dictionary<String, String> config, IServer server) : base(server)
		{
			Assert.Check(server is CustomQuantumServer);
			CustomServer = (CustomQuantumServer)server;
			_cmdHandler = new QuantumCommandHandler(this);
			CustomServer.OnSimulationCommand = _cmdHandler.DispatchLogicCommandFromQuantumEvent;
		}

		/// <summary>
		/// Called whenever any client sends an event
		/// </summary>
		public override void OnRaiseEvent(IRaiseEventCallInfo info)
		{
			if (info.Request.EvCode == (int)QuantumCustomEvents.Token)
			{
				info.Cancel();
				try
				{
					var token = Encoding.UTF8.GetString((byte[])info.Request.Data);
					_cmdHandler.ReceiveToken(info.ActorNr, token);
				} catch(Exception e)
				{
					Log.Error("Error reading user token "+e.Message);
				}
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
			if(FlgConfig.DebugMode)
			{
				Log.Info($"Actor {info.Request.ActorNr} joined with userId {info.UserId}");
			}
			
		}

		/// <summary>
		/// Called when a player creates a game.
		/// </summary>
		public override void OnCreateGame(ICreateGameCallInfo info)
		{
			try
			{

				if (FlgConfig.DebugMode)
				{
					Log.Info($"Actor {info.Request.ActorNr} created & joined with userId {info.UserId}");
				}

				if (!info.CreateOptions.TryGetValue("CustomProperties", out var propsObject))
				{
					base.OnCreateGame(info);
					Log.Debug("No Custom Properties");
					return;
				}

				var customProperties = propsObject as Hashtable;
				if (customProperties == null)
				{
					base.OnCreateGame(info);
					Log.Error("Game without custom properties");
					return;
				}

				_roomProps = new RoomProperties();

				// TODO: Not working for some reason
				//_roomProps.FromSystemHashTable(customProperties);
				//
				var allowedRewards = new List<GameId>();
				foreach (var idString in ((string)customProperties["alrewards"]).Split(','))
				{
					if (!string.IsNullOrEmpty(idString) && Enum.TryParse(idString, true, out GameId id))
					{
						allowedRewards.Add(id);
					}
				}

				// TODO: Validate if game is valid ranked using playfab matchmaking api, not hard to do 
				_roomProps.AllowedRewards.Value = allowedRewards;
				_roomProps.MatchType.Value = (MatchType)Enum.Parse(typeof(MatchType), (string)customProperties["mt"], true);

				// somebody trying to get noob from custom games fuck it!
				if (_roomProps.MatchType.Value == MatchType.Custom && _roomProps.AllowedRewards.Value.Contains(Quantum.GameId.NOOB))
				{
					info.Fail("invalid rewards");
					return;
					
				}

				if (CustomServer.ServerSimulation && _roomProps.MatchType.Value == MatchType.Custom)
				{
					if (FlgConfig.DebugMode)
					{
						Log.Info("Disabling simulation because custom game!");
					}

					CustomServer.ServerSimulation = false;
				}
				
				if (FlgConfig.DebugMode)
				{
					Log.Info($"Created {_roomProps.MatchType.Value.ToString()} game");
					Log.Info($"Allowed Rewards: {string.Join(",", _roomProps.AllowedRewards.Value)}");
				}

				base.OnCreateGame(info);
			}
			catch (Exception ex)
			{
				LogError(ex.ToString());
				info.Fail();
			}
		}

		/// <summary>
		/// Handler for after a game is closed.
		/// All players likely will already have left the room when the game is closed.
		/// This is the last step before the plugin is disposed.
		/// </summary>
		public override void OnCloseGame(ICloseGameCallInfo info)
		{
			CustomServer.Dispose();
			base.OnCloseGame(info);
		}

		public string MatchID => PluginHost.GameId;

		public RoomProperties RoomProperties => _roomProps;
	}
}
