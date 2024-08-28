using System;
using System.Collections.Generic;
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
		public string MatchId;
		public PlayerRef ExecutingPlayer;
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