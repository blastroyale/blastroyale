using System;
using FirstLight.Server.SDK.Services;
using Microsoft.AspNetCore.Hosting.Server;

namespace Tests.Stubs
{
	public class StubConfiguration : IBaseServiceConfiguration
	{
		public string AppPath { get; set; }
		public string ApplicationEnvironment { get; set; }
		public string PlayfabTitle { get; set; }
		public string PlayfabSecretKey { get; set; }
		public string? DbConnectionString { get; set; }
		public string? TelemetryConnectionString { get; set; }
		public Version? MinClientVersion { get; set; }
		public bool DevelopmentMode { get; set; }
		public bool Standalone { get; set; }
		public bool NftSync { get; set; }
		public bool RemoteGameConfiguration { get; set; }
		public string BuildCommit { get; set; }
		public string BuildNumber { get; set; }
		public string UnityCloudAuthToken { get; }
		public string UnityCloudEnvironmentName { get; }
		public string UnityCloudEnvironmentID { get; }
	}
}