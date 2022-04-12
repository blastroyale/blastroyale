
using Backend.Game;
using Backend.Game.Services;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlayFab;

[assembly: FunctionsStartup(typeof(Backend.Functions.FunctionStartup))]
namespace Backend.Functions;

/// <summary>
/// Server starup code. Here's where we will configure services specific, dependency injection among other
/// startup configurations.
/// </summary>
public class FunctionStartup : FunctionsStartup
{
	public override void Configure(IFunctionsHostBuilder builder)
	{
		var log = new LoggerFactory().CreateLogger("Log"); // TODO: Get proper azure log 
		IOCSetup.Setup(builder.Services, log);
	}
}
