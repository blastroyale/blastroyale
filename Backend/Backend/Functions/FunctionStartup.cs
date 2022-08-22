
using System;
using System.Data;
using System.Data.SqlTypes;
using Backend.Db;
using Backend.Game;
using Backend.Game.Services;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Services;
using Login.Db;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlayFab;

[assembly: FunctionsStartup(typeof(Backend.Functions.FunctionStartup))]

namespace Backend.Functions
{
	/// <summary>
	/// Server starup code. Here's where we will configure services specific, dependency injection among other
	/// startup configurations.
	/// </summary>
	public class FunctionStartup : FunctionsStartup
	{
		public override void Configure(IFunctionsHostBuilder builder)
		{
			ServerStartup.Setup(builder.Services, builder.GetContext().ApplicationRootPath);
		}
	}
}


