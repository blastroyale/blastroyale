namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Simple class to represent feature flags in the game.
	/// Not configurable in editor atm.
	/// </summary>
	public static class FeatureFlags
	{
		/// <summary>
		/// If true will use email/pass authentication.
		/// If false will only use device id authentication.
		/// </summary>
		public static readonly bool EMAIL_AUTH = false;

		/// <summary>
		/// If true, rooms created/joined will be locked by commit
		/// If false, player can create/join rooms not locked by commit
		/// </summary>
		public static bool COMMIT_VERSION_LOCK = true;

		/// <summary>
		/// When true will point to new production environments.
		/// </summary>
		public static bool TEMP_PRODUCTION_PLAYFAB = false;
	}
}