namespace Quantum
{
	public unsafe partial struct PlayerMatchData
	{
		/// <summary>
		/// Checks if this a valid player match data based on the defined data settings
		/// </summary>
		public bool IsValid => Entity != EntityRef.None;

		/// <summary>
		/// Checks if the current match data belongs to a bot
		/// </summary>
		public bool IsBot => BotNameIndex > 0;
	}
}