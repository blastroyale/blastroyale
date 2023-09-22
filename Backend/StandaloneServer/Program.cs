using System;
using System.IO;
using System.Text;
using Backend;
using Backend.Plugins;
using ServerCommon.Cloudscript;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PlayFab;
using PlayFab.CloudScriptModels;
using PlayFab.Json;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Server.SDK.Services;
using FirstLightServerSDK.Services;
using GameLogicService.Game;
using GameLogicService.Services;
using StandaloneServer;
using PluginManager = PlayFab.PluginManager;

// A minimalistic server wrapper for the game-server as a containerized rest api for local development & testing.

// Setup Logging
using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Trace).AddConsole());
ILogger logger = loggerFactory.CreateLogger<GameLogicWebWebService>();

// Setup Application
var builder = WebApplication.CreateBuilder(args);
var path = Path.GetDirectoryName(typeof(GameLogicWebWebService).Assembly.Location);
ServerStartup.Setup(builder.Services.AddMvc(), path);

// Remove database dependency for local run for simplicity and saving laptop cpu
builder.Services.RemoveAll(typeof(IServerMutex));
builder.Services.AddSingleton<IServerMutex, NoMutex>();

var app = builder.Build();

// Preloading configs and plugins
app.Services.GetService<IConfigsProvider>();
app.Services.GetService<IEventManager>();

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
	var shop = app.Services.GetService<ShopService>();
	var statistics = app.Services.GetService<IStatisticsService>();
	
	logger.LogInformation($"Logic Request Contents: {logicString?.ToString()}");
	
	// TODO: Make attribute that implements service calls in both Azure Functions and Standalone to avoid this
	PlayFabResult<BackendLogicResult?> result = functionRequest?.FunctionName switch
	{
		"ConsumeValidatedPurchaseCommand" => await shop.ProcessPurchaseRequest(playerId, logicRequest.Data["item_id"], bool.Parse(logicRequest.Data["fake_store"])),
		"RemovePlayerData"                => await webServer.RemovePlayerData(playerId),
		"ExecuteCommand"                  => await webServer.RunLogic(playerId, logicRequest),
		"GetPlayerData"                   => await webServer.GetPlayerData(playerId),
		"GetPublicProfile"                => Playfab.Result(playerId, await statistics.GetProfile(logicRequest.Command))
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
