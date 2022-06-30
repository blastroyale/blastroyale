using System.Text;
using Backend;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlayFab;
using PlayFab.CloudScriptModels;
using PlayFab.Json;
using ServerSDK.Services;
using StandaloneServer;

// A minimalistic server wrapper for the game-server as a containerized rest api for local development & testing.

// Setup Logging
using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Trace).AddConsole());
ILogger logger = loggerFactory.CreateLogger<Program>();

// Setup Application
var builder = WebApplication.CreateBuilder(args);
var path = Path.GetDirectoryName(typeof(ServerConfiguration).Assembly.Location);
ServerStartup.Setup(builder.Services, logger, path);

// Remove database dependency for local run for simplicity and saving laptop cpu
builder.Services.RemoveAll(typeof(IServerMutex));
builder.Services.AddSingleton<IServerMutex, NoMutex>();

var app = builder.Build();

app.MapGet("/", () => "Standalone Server is running !");

// Endpoint to simulate playfab's cloud script "ExecuteFunction/ExecuteCommand" locally.
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
	var webServer = app.Services.GetService<ILogicWebService>();
	// TODO: Make attribute that implements service calls in both Azure Functions and Standalone to avoid this
	PlayFabResult<BackendLogicResult?> result = functionRequest?.FunctionName switch
	{
		"SetupPlayerCommand" => await webServer.SetupPlayer(playerId),
		"ExecuteCommand" => await webServer.RunLogic(playerId, logicRequest),
		"GetPlayerData" => await webServer.GetPlayerData(playerId)
	};
	var res = new ExecuteFunctionResult()
	{
		FunctionName = "ExecuteCommand",
		FunctionResult = result
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