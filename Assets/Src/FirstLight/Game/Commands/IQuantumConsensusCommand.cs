using System;
using FirstLight.Game.Services;
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
		public bool Ranked;
	}
	
	/// <summary>
	/// Interface for service commands that requires to be agreed on inside a quantum game to be sent to server.
	/// All, or ost of players must agree with the same TConsensed data to be sent to be able to validate the data
	/// being sent is valid.
	/// The command will impersonate the sending player from server.
	/// </summary>
	public interface IQuantumConsensusCommand
	{
		/// <summary>
		/// Checks if the current command has consensus with other command
		/// </summary>
		bool HasConsensus(IQuantumConsensusCommand command);

		/// <summary>
		/// Set quantum values to the command. This will be enriched in client & quantum.
		/// To run command in client, client enriches this data there.
		/// To ensure server authority, server will set same values from their side.
		/// </summary>
		/// <param name="player"></param>
		void SetQuantumValues(QuantumValues values);

		/// <summary>
		/// Must return the issuer user session token to be validated server-side.
		/// </summary>
		public string GetSessionToken();
	}
}