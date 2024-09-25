namespace Quantum
{
	public static class QuantumFeatureFlags
	{
		/// <summary>
		/// If true, bots will only respect navmesh and will ignore all obstacles
		/// They can just pass trough things even tho they should respect navmesh
		/// </summary>
		public static bool BOTS_PHYSICS_IGNORE_OBSTACLES = true;

		public static bool FREEZE_BOTS = false;

		public static bool PLAYER_PUSHING = false;
		
		public static bool TEAM_IGNORE_COLLISION = false;
	}
}