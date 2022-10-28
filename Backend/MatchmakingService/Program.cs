using System;
using ContainerApp.Authentication;
using Firstlight.Matchmaking;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using ServerCommon;

PlayFabSettings.staticSettings.TitleId =
	Environment.GetEnvironmentVariable("PLAYFAB_TITLE", EnvironmentVariableTarget.Process);

PlayFabSettings.staticSettings.DeveloperSecretKey =
	Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
	options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme()
	{
		Name = "ApiKey",
		Type = SecuritySchemeType.ApiKey,
		Scheme = "ApiKey",
		BearerFormat = "ApiKey",
		In = ParameterLocation.Query,
		Description = "Api Key"
	});
	
});
var app = builder.Build();
app.UseCors(x => x
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .SetIsOriginAllowed(origin => true) // playfab origin is dynamic
                 .AllowCredentials());

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapControllers();
app.Run();