using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ServerShared.Authentication.ApiKey
{
	/// <summary>
	/// Controller decorator to determine that all services of the given controller requires the PlayFab secret
	/// to access this API. This should be used to any administrative services or services routed by third party (IE cloudscript)
	/// </summary>
	[AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method)]
	public class RequiresApiKey : Attribute, IAsyncActionFilter
	{
		private const string ApiKeyName = "key";
		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			if (!context.HttpContext.Request.Query.TryGetValue(ApiKeyName, out var extractedApiKey))
			{
				context.Result = new ContentResult()
				{
					StatusCode = 401,
					Content = "Api Key was not provided"
				};
				return;
			}
			var apiKey = Environment.GetEnvironmentVariable("API_KEY", EnvironmentVariableTarget.Process) ?? "devkey";
			if (!apiKey.Equals(extractedApiKey))
			{
				context.Result = new ContentResult()
				{
					StatusCode = 401,
					Content = "Api Key is not valid"
				};
				return;
			}
			await next();
		}
	}
}
