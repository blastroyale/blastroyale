using Assets.Src.FirstLight.Game.Commands.QuantumLogicCommands;
using FirstLight.Game.Commands;
using quantum.custom.plugin;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace Quantum
{ 
	public class QuantumCommandHandler
	{
		private CustomQuantumPlugin _plugin;
		private Dictionary<int, string> _tokens = new Dictionary<int, string>();
		private Assembly _commandAssembly;

		public QuantumCommandHandler(CustomQuantumPlugin plugin)
		{
			_plugin = plugin;
			_commandAssembly = Assembly.GetAssembly(typeof(EndOfGameCalculationsCommand));
		}

		public void DispatchLogicCommandFromQuantumEvent(EventFireQuantumServerCommand ev)
		{
			int index = ev.Player;
			if(_plugin.CustomServer.GetPlayFabIdByIndex(ev.Player) == null)
			{
				return;
			}

			var actorId = _plugin.CustomServer.GetClientActorNumberByIndex(index);
			if(FlgConfig.DebugMode)
			{
				Log.Info($"Firing logic command for index {index} actor {actorId}");
			}
			try
			{
				var logicCommand = QuantumLogicCommandFactory.BuildFromEvent(ev);
				if (logicCommand == null)
				{
					Log.Error("Could not instantiate command "+ev.CommandType);
				}
				DispatchCommand(actorId, logicCommand, ev.Game.Frames.Verified, true);
			}
			catch(Exception e)
			{
				Log.Exception(e);
			}
		}

		/// <summary>
		/// Reads the current frame data of the game simulation
		/// Enriches the command with data from this server-side simulation
		/// and dispatches the command to playfab
		/// </summary>
		public void DispatchCommand(int actorNumber, IQuantumCommand command, Frame frame, bool async = false)
		{
			if (_plugin.CustomServer.gameSession == null)
			{
				_plugin.LogError("Game did not ran, not sending commands");
				return;
			}

			if(!_tokens.TryGetValue(actorNumber, out var token))
			{
				_plugin.LogError($"User {actorNumber} did not send his token");
				return;
			}
			var playfabId = _plugin.CustomServer.GetPlayFabId(actorNumber);
			if(playfabId == null)
			{
				_plugin.LogError("Command without player id received");
				return;
			}
			var quantumValues = new QuantumValues()
			{
				ExecutingPlayer = _plugin.CustomServer.GetClientIndexByActorNumber(actorNumber),
				MatchType = _plugin.RoomProperties.MatchType.Value,
				AllowedRewards = _plugin.RoomProperties.AllowedRewards.Value,
				MatchId = _plugin.MatchID
			};
			command.FromFrame(frame, quantumValues);
			_plugin.CustomServer.Playfab.SendServerCommand(playfabId, token, command, async);
		}

		/// <summary>
		/// Receives a command from Unity to be enriched server-side and ran
		/// in our logic service in a full authoritative manner at the end of the game.
		/// </summary>
		public void ReceiveToken(int actorNumber, string token)
		{
			if(FlgConfig.DebugMode)
			{
				Log.Info($"Receive token for actor {actorNumber}");
			}
			_tokens[actorNumber] = token;
		}
	}
}
