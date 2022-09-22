using FirstLight.Game.Commands;
using FirstLight.Server.SDK.Modules;
using Photon.Deterministic;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Quantum
{ 
	public class QuantumCommandHandler
	{
		private CustomQuantumPlugin _plugin;
		private Dictionary<int, QuantumCommandPayload> _endGameCommands = new Dictionary<int, QuantumCommandPayload>();
		private Assembly _commandAssembly;

		public QuantumCommandHandler(CustomQuantumPlugin plugin)
		{
			_plugin = plugin;
			_commandAssembly = Assembly.GetAssembly(typeof(IGameCommand));
		}

		/// <summary>
		/// Sends all commands that were received
		/// </summary>
		public void DispatchAllCommands()
		{
			foreach(var actorNr in _endGameCommands.Keys)
			{
				DispatchCommand(actorNr, _endGameCommands[actorNr]);
			}
		}

		/// <summary>
		/// Reads the current frame data of the game simulation
		/// Enriches the command with data from this server-side simulation
		/// and dispatches the command to playfab
		/// </summary>
		public void DispatchCommand(int actorNumber, QuantumCommandPayload command)
		{
			if (_plugin.CustomServer.gameSession == null)
			{
				_plugin.LogError("Game did not ran, not sending commands");
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
				MatchType = _plugin.GetMatchType()
			};
			commandInstance.FromFrame(game.Frames.Verified, quantumValues);
			_plugin.CustomServer.Playfab.SendServerCommand(playfabId, command.Token, commandInstance, false);
		}

		/// <summary>
		/// Receives a command from Unity to be enriched server-side and ran
		/// in our logic service in a full authoritative manner at the end of the game.
		/// </summary>
		public void ReceiveEndGameCommand(int actorNumber, byte[] commandData)
		{
			var json = Encoding.UTF8.GetString(commandData);
			var command = ModelSerializer.Deserialize<QuantumCommandPayload>(json);
			var type = _commandAssembly.GetType(command.CommandType);
			_plugin.LogInfo($"Actor {actorNumber} sent command {type.Name}");
			_endGameCommands[actorNumber] = command;
		}
	}
}
