using System;

namespace FirstLight.Server.SDK.Services
{
	/// <summary>
	/// Main configuration properties for any gameplay service.
	/// </summary>
	public interface IBaseServiceConfiguration
	{
		/// <summary>
		/// Path of the running assemblies
		/// </summary>
		public string AppPath { get; }

		/// <summary>
		/// Application environment, dev,staging,testnet,mainnet
		/// This are not the actual values, need to check the deploy pipelines
		/// </summary>
		public string ApplicationEnvironment { get; }

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
		/// On standalone, no other external dependencies are needed for requests
		/// like playfab or postgres
		/// </summary>
		bool Standalone { get; }

		/// <summary>
		/// Enables or disabled NFT sync system
		/// </summary>
		bool NftSync { get; }

		/// <summary>
		/// When true will fetch the configuration from a remote backend.
		/// If false will fetch config baked into the server.
		/// </summary>
		bool RemoteGameConfiguration { get; }

		/// <summary>
		/// Git commit used to build this image
		/// </summary>
		string BuildCommit { get; }

		/// <summary>
		/// Devops build number used to build
		/// </summary>
		string BuildNumber { get; }

		/// <summary>
		/// The basic auth token for the Unity Token Exchange API
		/// </summary>
		public string UnityCloudAuthToken { get; }

		/// <summary>
		/// The name of the environment in Unity Cloud (development, staging, production)
		/// </summary>
		public string UnityCloudEnvironmentName { get; }

		/// <summary>
		/// The ID of the environment in Unity Cloud.
		/// </summary>
		public string UnityCloudEnvironmentID { get; }
	}
}