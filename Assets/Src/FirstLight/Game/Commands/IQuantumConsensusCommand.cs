using System;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Represents quantum data that will be set by client/server. Data in this model are intended to be server authorative
	/// driven by Quantum Server.
	/// Client will input data for local prediction where server will validate and pass this data to the command.
	/// This model is enriched in client and on QuantumServer. Whenever adding/removing things, remember to update Quantum Server.
	/// </summary>
	[Serializable]
	public struct QuantumValues
	{
		public PlayerRef ExecutingPlayer;
		public MatchType MatchType;
	}

	/// <summary>
	/// Payload with needed information to be sent to quantum server to run quantum commands.
	/// We dont need the command data, as we enrich the command server-side fully in authoritative manner.
	/// </summary>
	[Serializable]
	public struct QuantumCommandPayload
	{
		public string Token;
		public string CommandType;
	}
	
	/// <summary>
	/// Quantum Commands will be executed on client and also by quantum server at the end of every match.
	/// </summary>
	public interface IQuantumCommand
	{
		/// <summary>
		/// Fills command data from a given frame for a given player.
		/// This will be called in client & quantum server.
		/// </summary>
		void FromFrame(Frame frame, QuantumValues QuantumValues);
	}
}