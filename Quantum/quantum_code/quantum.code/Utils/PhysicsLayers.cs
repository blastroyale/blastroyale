namespace Quantum
{
	public static class PhysicsLayers
	{
		/// <summary>
		/// Responsible for the player hitbox, the collider you as a player would visually see on the screen
		/// It's the collider we will collide hits like bullets and hammer with so they are visually accurate
		/// </summary>
		public static readonly string PLAYERS_HITBOX = "PlayersHitbox";
		
		/// <summary>
		/// This is the actual player collider which is represented by the 3d space the player feet is
		/// We use this for movement collisions like bushes, entering buildings, reviving, collecting items etc
		/// </summary>
		public static readonly string PLAYERS = "Players";
		
		public static readonly string BULLETS = "Bullets";
		public static readonly string OBSTACLES = "Obstacles";
		public static readonly string PLAYER_TRIGGERS = "CollideOnlyWithPlayers";
	}
}