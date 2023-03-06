namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Constants related to the Playfab API
	/// </summary>
	public class PlayFabConstants
	{
		/// <summary>
		/// Entity type of the title, it is used for things like party and matchmaking
		/// </summary>
		public const string TITLE_PLAYER_ENTITY_TYPE = "title_player_account";
		/// <summary>
		/// This is the account type used in authentication and also sent to the simulation
		/// </summary>
		public const string MASTER_PLAYER_ENTITY_TYPE = "master_player_account";
	}
}