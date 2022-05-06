using System.IO;
using System.Text;
using Backend.Db;
using Backend.Game;
using FirstLight.Game.Logic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlayFab;
using PlayFab.CloudScriptModels;
using PlayFab.Json;
using StandaloneServer;

// A minimalistic server wrapper for the game-server as a containerized rest api for local development & testing.

// Setup Logging
using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Trace).AddConsole());
ILogger logger = loggerFactory.CreateLogger<Program>();

// Setup Application
var builder = WebApplication.CreateBuilder(args);
IOCSetup.Setup(builder.Services, logger);
DbSetup.Setup(builder.Services);
var app = builder.Build();

app.MapGet("/", () => "Standalone Server is running !");

// Endpoint to simulate playfab's cloud script "ExecuteFunction/ExecuteCommand" locally. Works only with execute command.
app.MapPost("/CloudScript/ExecuteFunction", async (ctx) =>
{
	logger.LogInformation("Request received");
	var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
	using var sr = new StreamReader(ctx.Request.Body, Encoding.UTF8);
	var contents = await sr.ReadToEndAsync();
	var functionRequest = serializer.DeserializeObject<ExecuteFunctionRequest>(contents);
	var playerId = functionRequest?.AuthenticationContext.PlayFabId;
	var logicString = functionRequest?.FunctionParameter as JsonObject;
	var logicRequest = serializer.DeserializeObject<LogicRequest>(logicString?.ToString());
	var res = new ExecuteFunctionResult()
	{
		FunctionName = "ExecuteCommand",
		FunctionResult = new PlayFabResult<BackendLogicResult?>
		{
			Result =  app.Services.GetService<GameServer>()?.RunLogic(playerId, logicRequest)
		}
	};
	ctx.Response.StatusCode = 200;
	await ctx.Response.WriteAsync(serializer.SerializeObject(new PlayfabHttpResponse()
	{
		code = 200,
		status = "OK",
		data = res
	}));
});

app.Run();