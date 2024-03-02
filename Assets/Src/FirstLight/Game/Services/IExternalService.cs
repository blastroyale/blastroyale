namespace FirstLight.Game.Services
{
	/// <summary>
	/// Interface for game services that can be consumed in the game but are implemented by code
	/// that is maintained completely outside of the game and should not generate dependency to the game.
	/// </summary>
	public interface IExternalService
	{
		/// <summary>
		/// Returns if this service is available or not.
		/// A inavailable service can be due to permissions, feature flags, lack of implementation on
		/// environments etc
		/// </summary>
		bool IsServiceAvailable { get; }
	}
}