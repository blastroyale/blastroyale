using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace ContainerApp.Authentication
{
	/// <summary>
	/// Asp.Net middleware to require secret keys on specific endpoints.
	/// </summary>
	public class ApiKeyMiddleware
	{
		private readonly RequestDelegate? _next;
		private readonly IConfiguration? _appSettings;

		public ApiKeyMiddleware(RequestDelegate? next, IConfiguration? appSettings)
		{
			_next = next;
			_appSettings = appSettings;
		}

		public async Task Invoke(HttpContext context)
		{
			if (context.Request.Query.ContainsKey("key"))
			{
				var token = context.Request.Query["key"].ToString();
				context.Items["api_key"] = token;
			}
			if(_next != null)
				await _next(context);
		}
	}
}

