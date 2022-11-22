using System;
using System.IO;
using GameLogicApp.Authentication;
using Firstlight.Matchmaking;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlayFab;
using ServerCommon;

PlayFabSettings.staticSettings.TitleId =
	Environment.GetEnvironmentVariable("PLAYFAB_TITLE", EnvironmentVariableTarget.Process);

PlayFabSettings.staticSettings.DeveloperSecretKey =
	Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process);

var config = new MatchmakingConfig("flglobby", "flgranked");

var binPath = Path.GetDirectoryName(typeof(MatchmakingController).Assembly.Location);
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().SetupSharedServices(binPath);
builder.Services.AddSingleton<IErrorService<PlayFabError>, PlayfabErrorService>();
builder.Services.AddSingleton<IMatchmakingServer, PlayfabMatchmakingServer>();
builder.Services.AddSingleton<MatchmakingConfig>(p => config);

var app = builder.Build();

app.UseCors(x => x.AllowAnyMethod()
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

public partial class Program
{
}