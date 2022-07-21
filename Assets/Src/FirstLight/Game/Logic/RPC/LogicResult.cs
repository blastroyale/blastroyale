using System;
using System.Collections.Generic;
using PlayFab.SharedModels;

namespace FirstLight.Game.Logic.RPC
{
	/// <summary>
	/// This object defines the result of an function app logic execution
	/// </summary>
	[Serializable]
	public class LogicResult : PlayFabResultCommon
	{
		/// <summary>
		/// Player Id
		/// </summary>
		public string PlayFabId;
		/// <summary>
		/// The command executed
		/// </summary>
		public string Command;
		/// <summary>
		/// Extra Data to return back to the client executed from the logic request
		/// </summary>
		public Dictionary<string, string> Data;
	}
}