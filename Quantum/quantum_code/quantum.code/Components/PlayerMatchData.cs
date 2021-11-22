using System;
using Quantum.Collections;

namespace Quantum
{
	public unsafe partial struct PlayerMatchData
	{
		/// <summary>
		/// Checks if this a valid player match data based on the defined data settings
		/// </summary>
		public bool IsValid => Entity != EntityRef.None;
	}
}