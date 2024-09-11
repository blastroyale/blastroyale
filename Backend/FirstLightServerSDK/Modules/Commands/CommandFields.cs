namespace FirstLight.Server.SDK.Modules.Commands
{
	/// <summary>
	/// Refers to dictionary keys used in the data sent to server.
	/// </summary>
	public static class CommandFields
	{
		/// <summary>
		/// Target player of the commmand, not to be confused with the player running the command
		/// </summary>
		public static readonly string PlayerId = nameof(PlayerId);

		/// <summary>
		/// The command type to be executed
		/// </summary>
		public static readonly string CommandType = nameof(CommandType);

		/// <summary>
		/// Key where the command data is serialized.
		/// </summary>
		public static readonly string CommandData = nameof(IGameCommand);

		/// <summary>
		/// Field containing the client timestamp for when the command was issued.
		/// </summary>
		public static readonly string Timestamp = nameof(Timestamp);

		/// <summary>
		/// Field about the version the game client is currently running
		/// </summary>
		public static readonly string ClientVersion = nameof(ClientVersion);

		/// <summary>
		/// Field that represents the client configuration version
		/// </summary>
		public static readonly string ServerConfigurationVersion = nameof(ServerConfigurationVersion);

		/// <summary>
		/// Field that represents the client configuration version
		/// </summary>
		public static readonly string ErrorCode = nameof(ErrorCode);
	}
}