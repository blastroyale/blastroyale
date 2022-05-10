using System;
using System.Data;
using Backend.Game;
using Login.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Db;

/// <summary>
/// Sets up database dependency injection contexts and any database interfaces that are required.
/// Also will validate and except if any configuration is missing.
/// </summary>
public static class DbSetup
{
	public static string ConnectionString =>
		Environment.GetEnvironmentVariable("SqlConnectionString", EnvironmentVariableTarget.Process);
	
	/// <summary>
	/// Sets up database for the game server. Will setup any specific database dependencies that have to be injected.
	/// </summary>
	public static void Setup(IServiceCollection services)
	{
		if (ConnectionString == null)
		{
			throw new DataException("Database not configured. Please set 'SqlConnectionString' env var");
		}
		services.AddDbContext<PlayersContext>(o => o.UseNpgsql(ConnectionString));
		services.AddSingleton<IServerMutex, PostgresMutex>();
	}
}