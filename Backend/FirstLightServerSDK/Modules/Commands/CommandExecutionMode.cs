namespace FirstLight.Server.SDK.Modules.Commands
{
	
	public enum CommandExecutionMode
	{
		/// <summary>
		/// Only runs the command in client.
		/// </summary>
		ClientOnly,
		
		/// <summary>
		/// Marks this command to be executed only on the client or also on the server.
		/// By default <see cref="IGameCommand"/> always runs on the server. To only run on the client, please mark on
		/// the interface implementation as false.
		/// </summary>
		/// <remarks>
		/// Use this check with care and guarantee that will not create de-syncs between the local and server state
		/// </remarks>
		Server,
		
		/// <summary>
		/// Runs the command from a given frame, in client & server. Commands are not transported via network
		/// on those cases and are completely fired from server simulation by a given frame.
		/// </summary>
		Quantum,
		
		/// <summary>
		/// Commands which run automatically only on the server after player authentication
		/// </summary>
		Initialization
	}
}
