using System;
using Unity.Services.CloudCode.Core;

namespace Scripts.Base;

public class UnityScriptExecutionContext : IExecutionContext
{
	public string ProjectId { get; set; }
	public string? PlayerId { get; set; }
	public string EnvironmentId { get; set; }
	public string EnvironmentName { get; set; }
	public string AccessToken { get; set; }
	public string? UserId { get; set; }
	public string? Issuer { get; set; }
	public string ServiceToken { get; set; }
	public string? AnalyticsUserId { get; set; }
	public string? UnityInstallationId { get; set; }
	public string CorrelationId { get; set; }

	public Guid ProjectGuid => new Guid(ProjectId);
	public Guid EnvironmentGuid => new Guid(EnvironmentId);
	public string ServiceAccountKeyId;
	public string ServiceAccountSecretKey;
}