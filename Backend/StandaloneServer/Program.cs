using System;
using System.IO;
using System.Linq;
using Backend;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Server.SDK.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ServerCommon;
using StandaloneServer;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
	builder.Services.AddHttpLogging(options =>
	{
		options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
			HttpLoggingFields.RequestBody;
	});
}

if (builder.Environment.IsDevelopment())
{
	builder.Services.AddHttpLogging(options =>
	{
		options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
			HttpLoggingFields.RequestBody;
	});
}

var binPath = Path.GetDirectoryName(typeof(GameLogicWebWebService).Assembly.Location);
ServerStartup.Setup(builder.Services.AddControllers().AddApplicationPart(typeof(AppController).Assembly).AddControllersAsServices(), binPath);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.RemoveAll(typeof(IServerMutex));
builder.Services.AddSingleton<IServerMutex, NoMutex>();

var app = builder.Build();
app.UseCors(x => x
	.AllowAnyMethod()
	.AllowAnyHeader()
	.SetIsOriginAllowed(origin => true) // playfab origin is dynamic
	.AllowCredentials());

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapControllers();

// Preloading configs & plugins
app.Services.GetService<IConfigsProvider>();
app.Services.GetService<IEventManager>();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Delicious API Testing Page: http://localhost:7274/swagger/index.html");
Console.ResetColor();

app.Run();