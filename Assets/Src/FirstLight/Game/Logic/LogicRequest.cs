using System.Collections.Generic;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This object defines the request structure to execution a logic function
	/// </summary>
	public class LogicRequest
	{
		/// <summary>
		/// The command logic to execute.
		/// Each command has it's own logic to execute on the PlayFab Backend.
		/// </summary>
		public string Command;
		/// <summary>
		/// The Data to update on the given <see cref="LogicRequest.Command"/> logic request
		/// </summary>
		public Dictionary<string, string> Data;
		/// <summary>
		/// The List of keys to delete from the player data on the given <see cref="LogicRequest.Command"/> logic request
		/// </summary>
		public List<string> RemoveKeys;
		/// <summary>
		/// Requests the platform the request was invoked
		/// </summary>
		public string Platform;
	}
}