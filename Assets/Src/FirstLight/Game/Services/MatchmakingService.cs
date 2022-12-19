using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Stores any player selections along the way until the game starts
	/// </summary>
	public interface IMatchmakingService
	{
		Vector2 NormalizedMapSelectedPosition { get; set; }
	}
	
	/// <inheritdoc cref="IMatchmakingService"/>
	public class MatchmakingService : IMatchmakingService
	{
		/// <summary>
		/// Returns the player's selected point on the map in a normalized state
		/// </summary>
		public Vector2 NormalizedMapSelectedPosition { get; set; }
	}
}