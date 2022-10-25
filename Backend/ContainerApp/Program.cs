using System;
using System.IO;
using Backend;
using Backend.Game.Services;
using ContainerApp.Authentication;
using FirstLight.Server.SDK.Models;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
	builder.Services.AddHttpLogging(options =>
	{
		options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
								HttpLoggingFields.RequestBody;
	});
}

var binPath = Path.GetDirectoryName(typeof(GameLogicWebWebService).Assembly.Location);
ServerStartup.Setup(builder.Services.AddControllers(), binPath);

var app = builder.Build();
app.UseCors(x => x
     .AllowAnyMethod()
     .AllowAnyHeader()
     .SetIsOriginAllowed(origin => true) // playfab origin is dynamic
     .AllowCredentials());


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapControllers();
app.Run();