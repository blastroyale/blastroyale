using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Holds an displayable reward, it uses the GameId to get the sprite
	/// </summary>
	public interface IReward
	{
		/// <summary>
		/// Used to load the sprite and also get the translated name
		/// </summary>
		GameId GameId { get; }

		/// <summary>
		/// Amount displayed at the views
		/// </summary>
		uint Amount { get; }

		/// <summary>
		/// Used in the views to display the reward name
		/// </summary>
		string DisplayName { get; }
	}
}