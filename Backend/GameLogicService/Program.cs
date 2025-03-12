using System;
using System.IO;
using Backend;
using FirstLight.Game.Data;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Server.SDK.Services;
using GameLogicService;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using ServerCommon.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

var binPath = Path.GetDirectoryName(typeof(GameLogicWebWebService).Assembly.Location);
var env = ServerStartup.Setup(builder.Services.AddControllers().AddControllersAsServices(), binPath);

builder.SetupLogging(env);
builder.SetupMetrics(env);

if (env.Standalone)
{
	Console.WriteLine("Initializing Standalone Server");
	builder.Services.AddHttpLogging(options =>
	{
		options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
			HttpLoggingFields.RequestBody;
	});

	builder.Services.AddEndpointsApiExplorer();
	builder.Services.AddSwaggerGen();
	if (string.IsNullOrEmpty(env.RedisLockConnectionString))
	{
		builder.Services.RemoveAll(typeof(IDistributedLockProvider));
		builder.Services.AddSingleton<IDistributedLockProvider>(provider =>
		{
			var lockFileDirectory =
				new DirectoryInfo(Environment.CurrentDirectory +
					"/Temp_Locks"); // choose where the lock files will live
			return new FileDistributedSynchronizationProvider(lockFileDirectory);
		});
	}
}

var app = builder.Build();

ServerStartup.PostLoad(app.Services);

var envConfig = app.Services.GetService<IBaseServiceConfiguration>();
if (envConfig.Standalone)
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

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

// Preloading configs & plugins
app.Services.GetService<IConfigsProvider>();
app.Services.GetService<IEventManager>();

if (envConfig.Standalone)
{
	Console.ForegroundColor = ConsoleColor.Green;
	Console.WriteLine("Game Logic Server Running: http://localhost:7274/swagger/index.html");
	Console.ResetColor();
}

app.Run();

public partial class Program
{
} // make it accessible in tests