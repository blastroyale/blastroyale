using System;

namespace FirstLight.Server.SDK.Services
{
	/// <summary>
	/// Main configuration properties for the game logic service.
	/// </summary>
	public interface IServerConfiguration
	{
		/// <summary>
		/// Path of the running assemblies
		/// </summary>
		public string AppPath { get; }
		
		/// <summary>
		/// Playfab title the server will comunicate to
		/// </summary>
		public string PlayfabTitle { get; }
		
		/// <summary>
		/// Secret key of the given playfab title
		/// </summary>
		public string PlayfabSecretKey { get; }
		
		/// <summary>
		/// Database connection string.
		/// </summary>
		string? DbConnectionString { get; }
		
		/// <summary>
		/// Telemetry connection string to send app insights, sentry whichever telemetry
		/// </summary>
		string? TelemetryConnectionString { get; }
		
		/// <summary>
		/// Minimal client version required for the server to communicate to.
		/// TODO: Remove this when server is client agnostic.
		/// </summary>
		Version? MinClientVersion { get; }
		
		/// <summary>
		/// Whenever development mode is enabled, permission checks can be bypassed
		/// </summary>
		bool DevelopmentMode { get; }
		
		/// <summary>
		/// Enables or disabled NFT sync system
		/// </summary>
		bool NftSync { get; }
		
		/// <summary>
		/// When true will fetch the configuration from a remote backend.
		/// If false will fetch config baked into the server.
		/// </summary>
		bool RemoteGameConfiguration { get; }
	}
}