using Assets.Src.FirstLight.Game.Commands.QuantumLogicCommands;
using FirstLight.Game.Commands;
using FirstLight.Server.SDK.Modules;
using quantum.custom.plugin;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FirstLight.Server.SDK.Modules.Commands;

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
				var payload = new QuantumCommandPayload()
				{
					CommandType = logicCommand.GetType().FullName
				};
				DispatchCommand(actorId, payload, ev.Game.Frames.Verified, true);
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
		public void DispatchCommand(int actorNumber, QuantumCommandPayload command, Frame frame, bool async = false)
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

			var game = _plugin.CustomServer.gameSession.Session.Game as QuantumGame;
			var commandType = _commandAssembly.GetType(command.CommandType);
			if(commandType == null)
			{
				_plugin.LogError($"Could not find command type {command.CommandType}");
				return;
			}
			var commandInstance = Activator.CreateInstance(commandType) as IQuantumCommand;
			if (commandInstance == null)
			{
				_plugin.LogError($"Actor {actorNumber} sent command {commandType.Name} which is not a quantum command");
				return;
			}
			
			var quantumValues = new QuantumValues()
			{
				ExecutingPlayer = _plugin.CustomServer.GetClientIndexByActorNumber(actorNumber),
				MatchType = _plugin.RoomProperties.MatchType.Value,
				AllowedRewards = _plugin.RoomProperties.AllowedRewards.Value,
				MatchId = _plugin.MatchID
			};
			commandInstance.FromFrame(frame, quantumValues);
			_plugin.CustomServer.Playfab.SendServerCommand(playfabId, token, commandInstance, async);
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
