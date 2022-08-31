using FirstLight.Game.Commands;
using Photon.Deterministic;
using Photon.Deterministic.Server.Interface;
using Photon.Hive.Plugin;
using quantum.custom.plugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Quantum
{
	/// <summary>
	/// Class represents a quantum plugin. It wraps around a server.
	/// This instance is created by PluginFactory everytime a match starts.
	/// </summary>
	public class CustomQuantumPlugin : DeterministicPlugin
	{
		private readonly CustomQuantumServer _server;
		private readonly EndGameCommandConsensusHandler _consensus;
		private List<int> _actorsOnlineWhenLastCommandReceived;
		private bool _isRanked;

		public CustomQuantumPlugin(Dictionary<String, String> config, IServer server) : base(server)
		{
			Assert.Check(server is CustomQuantumServer);
			_consensus = new EndGameCommandConsensusHandler();
			_server = (CustomQuantumServer)server;
			_actorsOnlineWhenLastCommandReceived = new List<int>();
			if(config.TryGetValue("ForceRanked", out var forceRanked) && forceRanked == "true")
			{
				_isRanked = true;
				Log.Info("Forcing match as a ranked match");
			}
			if (config.TryGetValue("TestConsensus", out var testConsensus) && testConsensus == "true")
			{
				FlgConfig.TEST_CONSENSUS = true;
				Log.Info("Test Consensus Mode");
			}

		}

		/// <summary>
		/// Called whenever any client sends an event
		/// </summary>
		public override void OnRaiseEvent(IRaiseEventCallInfo info)
		{
			if (info.Request.EvCode == (int)QuantumCustomEvents.ConsensusCommand)
			{
				info.Cancel();
				var client = _server.GetClientForActor(info.ActorNr);
				if (client == null)
				{
					return;
				}
				if (info.Request.Data == null)
				{
					_server.DisconnectClient(client, "Invalid command data");
					return;
				}
				var bytes = (byte[])info.Request.Data;
				var cmd = _consensus.DeserializeCommand(bytes);
				_actorsOnlineWhenLastCommandReceived = _server.GetActorIds().ToList();
				_consensus.ReceiveCommand(client.ActorNr, cmd);
				Log.Info($"Actor {client.ActorNr} Index {_server.GetClientIndexByActorNumber(client.ActorNr)} PlayerId {_server.GetPlayFabId(client.ActorNr)} submited consensus end-game command");
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
			if (FlgConfig.TEST_CONSENSUS)
			{ 
				_isRanked = true;
			}
			if (!info.CreateOptions.TryGetValue("CustomProperties", out var propsObject))
			{
				Log.Debug("No Custom Properties");
				return;
			}
			var customProperties = propsObject as Hashtable;
			if(customProperties==null)
			{
				Log.Debug("No Custom Properties");
				return;
			}
			if(!customProperties.ContainsKey("isRanked"))
			{
				Log.Debug("No 'isRanked' flag on custom properties");
				return;
			}
			if (!(bool)customProperties["isRanked"])
			{
				Log.Debug("Not a ranked game");
				return;
			}
			_isRanked = true;
			Log.Info("Ranked Game Created");
		}

		/// <summary>
		/// Called whenever a player leaves the room. 
		/// Will be called for every player before EndGame is called.
		/// </summary>
		public override void OnLeave(ILeaveGameCallInfo info)
		{
			if(FlgConfig.TEST_CONSENSUS) // for faster test cycles
			{
				RunCommandConsensus();
			}
			base.OnLeave(info);
		}

		/// <summary>
		/// Handler for after a game is closed.
		/// All players likely will already have left the room when the game is closed.
		/// This is the last step before the plugin is disposed.
		/// </summary>
		public override void OnCloseGame(ICloseGameCallInfo info)
		{
			RunCommandConsensus();
			_consensus.Dispose();
			_server.Dispose();
			_actorsOnlineWhenLastCommandReceived.Clear();
			base.OnCloseGame(info);
		}

		/// <summary>
		/// An actor is valid if the time he left the game was at least 'SECONDS_BEFORE_END_TRESHHOLD' seconds ago.
		/// If the actor left the game long ago, he is not valid and his command should not be taken to consensus to
		/// avoid screwing the other players.
		/// </summary>
		private bool IsActorValid(int actorNr)
		{
			return _actorsOnlineWhenLastCommandReceived.Contains(actorNr);
		}

		private QuantumValues GetCommandQuantumValues(int actorNr)
		{
			return new QuantumValues()
			{
				ExecutingPlayer = new PlayerRef()
				{
					_index = _server.GetClientIndexByActorNumber(actorNr),
				},
				Ranked = _isRanked
			};
		}

		/// <summary>
		/// Runs the command consensus matching.
		/// If a consensus is reached, commands are dispached for every respective player.
		/// </summary>
		private void RunCommandConsensus()
		{
			var validActors = _server.GetValidatedPlayers();
			var actorsQuitEarly = new List<int>();

			var validActorCount = validActors.Count;
			if (validActorCount < FlgConfig.MIN_PLAYERS)
			{
				Log.Error($"Game ended with less than {FlgConfig.MIN_PLAYERS}");
				return;
			}

			Log.Info($"Running command consensus with actors {string.Join(",", validActors)}");
			Log.Info($"Actors still connected when last command sent: {string.Join(", ", _actorsOnlineWhenLastCommandReceived)}");

			foreach (var actorNr in new List<int>(validActors.Keys))
			{
				if(!IsActorValid(actorNr))
				{
					Log.Error($"Invalid actor {actorNr}");
					validActors.Remove(actorNr);
					actorsQuitEarly.Add(actorNr);
				}
			}

			var minPlayersForConsensus = validActorCount <= FlgConfig.MIN_PLAYERS_100PCT ? validActorCount : (int)(validActorCount * FlgConfig.CONSENSUS_PCT);
			var agreed = _consensus.GetConsensus(minPlayersForConsensus);
			if(agreed == null)
			{
				if(validActors.Count > 0)
				{
					Log.Error($"{validActors} players failed to reach end of game data consensus");
				}
			}
			
			if (agreed != null)
			{
		
				Log.Info($"Consensus end of game agreed among {agreed.Count} players (min {minPlayersForConsensus})");
				foreach (var actorNr in agreed.Keys)
				{
					var command = agreed[actorNr];
					command.QuantumValues = GetCommandQuantumValues(actorNr);
					var playfabId = _server.GetPlayFabId(actorNr);
					_server.Playfab.SendServerCommand(playfabId, command.PlayfabToken, command);
				}

				if (actorsQuitEarly.Count > 0)
				{
					Log.Info($"Sending {actorsQuitEarly} unconsensed commands for quitters");
					var validDataCommand = agreed.Values.First();
					foreach (var quitter in actorsQuitEarly)
					{
						var command = _consensus.Commands[quitter];
						// since this was not part of the consensus, for quitters we copy the data
						// from the consensed command
						command.PlayersMatchData = validDataCommand.PlayersMatchData;
						command.QuantumValues = GetCommandQuantumValues(quitter);
						Log.Debug($"Actor {quitter} index {command.QuantumValues.ExecutingPlayer._index} sending quitter cmd");
						var playfabId = _server.GetPlayFabId(quitter);
						_server.Playfab.SendServerCommand(playfabId, command.PlayfabToken, command);

					}

				}
			}
		}
	}
}
