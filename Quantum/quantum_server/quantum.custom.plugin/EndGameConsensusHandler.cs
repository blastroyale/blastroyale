using System;
using System.Collections.Generic;
using System.Text;
using FirstLight.Game.Commands;
using FirstLight.Game.Utils;
using System.Linq;
using ServerSDK.Modules;

namespace Quantum
{

	/// <summary>
	/// This object is only needed due to being a hack. We should be using game dll command class here
	/// however when we run Photon server, generic typing structs raises some errors likely due to 
	/// different processor targets. This structure is strong typed with the same fields of EndGameCommand 
	/// 
	/// Error Information:
	/// 
	/// System.TypeLoadException: Non-abstract, non-.cctor method in an interface.
	/// Microsoft.Common.CurrentVersion.targets(2302,5): warning MSB3270: 
	/// There was a mismatch between the processor architecture of the project being built "MSIL" and the processor architecture of the reference 
	/// "PhotonHivePlugin", "AMD64". 
	/// This mismatch may cause runtime failures. Please consider changing the targeted processor architecture of your project 
	/// through the Configuration Manager so as to align the processor architectures between your project and references, 
	/// or take a dependency on references with a processor architecture that matches the targeted processor architecture of your project.
	/// </summary>
	[Serializable]
	public struct EndGameConsensusCommandData
	{
		public string PlayfabToken;
		public List<QuantumPlayerMatchData> PlayersMatchData;
		public QuantumValues QuantumValues;

		public bool HasConsensus(EndGameConsensusCommandData endCommand)
		{
			var myHashes = PlayersMatchData.Select(d => d.GetHashCode());
			var hisHashes = endCommand.PlayersMatchData.Select(d => d.GetHashCode());
			return myHashes.SequenceEqual(hisHashes);
		}
	}

	/// <summary>
	/// Consensus handler. Will take in command bytes, deserialize and reach a consensus of which of those commands agreed with eachother.
	/// All commands sent (or almost all) have to have the same consensus, when a consensus is reached those commands are sent to be executed to their 
	/// respective players.
	/// </summary>
	public class EndGameCommandConsensusHandler : IDisposable
	{
		private Dictionary<int, EndGameConsensusCommandData> _commandsReceived;

		public EndGameCommandConsensusHandler()
		{
			_commandsReceived = new Dictionary<int, EndGameConsensusCommandData>();
		}

		public Dictionary<int, EndGameConsensusCommandData> Commands { get => _commandsReceived; }

		/// <summary>
		/// Adds a command to the commands received list.
		/// </summary>
		public void ReceiveCommand(int actorNr, EndGameConsensusCommandData command) => _commandsReceived[actorNr] = command;

		/// <summary>
		/// From the received commands, try to obtain a consensus of at least 'minPlayers' amount.
		/// If a consensus is reached, all commands that reached that consensus are returned.
		/// Returns null when no consensus is reached.
		/// </summary>
		public Dictionary<int, EndGameConsensusCommandData> GetConsensus(int minPlayers)
		{
			if (_commandsReceived.Count < minPlayers)
			{
				Log.Error($"{_commandsReceived.Count} consensus commands reveiced, {minPlayers} needed");
				return null;
			}

			foreach (var actorNr in _commandsReceived.Keys)
			{
				var agreed = new Dictionary<int, EndGameConsensusCommandData>();
				foreach (var otherActorNr in _commandsReceived.Keys)
				{
					var otherCommand = _commandsReceived[otherActorNr];
					if (_commandsReceived[actorNr].HasConsensus(otherCommand))
					{
						Log.Debug($"Actor {actorNr} Agreed with {otherActorNr}");
						agreed[otherActorNr] = otherCommand;
					} else
					{
						Log.Debug($"Actor {actorNr} Disagreed with {otherActorNr}");
					}
				}
				if (agreed.Count >= minPlayers)
				{
					return agreed;
				}
			}
			return null;
		}

		/// <summary>
		/// Deserialize the command. Should deserialize a IQuantumConsensusCommand
		/// But due to an issue could not use IQuantumConsensusCommand type.
		/// As a workaround a simillar type struct used to handle the command internally.
		/// </summary>
		public EndGameConsensusCommandData DeserializeCommand(byte[] cmdBytes)
		{
			var json = Encoding.UTF8.GetString(cmdBytes);
			var spliint = json.IndexOf(":");
			var commandData = json.Substring(spliint + 1, json.Length - spliint - 1);
			return ModelSerializer.Deserialize<EndGameConsensusCommandData>(commandData);
		}

		/// <summary>
		/// Disposes the consensus command handler by removing all received commands.
		/// </summary>
		public void Dispose()
		{
			_commandsReceived.Clear();
		}

	}
}