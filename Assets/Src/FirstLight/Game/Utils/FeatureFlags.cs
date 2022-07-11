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
		/// If true the game will require player to equip 3 NFT's to play the game
		/// If false the game will NOT require player to equip 3 NFTsS to play the game
		/// </summary>
		public static readonly bool NFT_REQ = false;
	}
}