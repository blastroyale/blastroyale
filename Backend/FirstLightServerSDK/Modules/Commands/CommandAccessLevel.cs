namespace FirstLight.Server.SDK.Modules.Commands
{
	/// <summary>
	/// Defines the required user permission level to access a given command.
	/// </summary>
	public enum CommandAccessLevel
	{
		/// <summary>
		/// Standard permission, allows command to be ran only for the given authenticated player.
		/// </summary>
		Player,

		/// <summary>
		/// Only allows the command to be ran for the given authenticated player but Admin commands might
		/// perform operations normal players can't like cheats.
		/// </summary>
		Admin,

		/// <summary>
		/// Service commands might be used for any given player without requiring player authentication.
		/// It will impersonate a player to run the command from a third party service.
		/// Will require a secret key to run the command.
		/// </summary>
		Service
	}
}

