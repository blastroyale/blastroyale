using System;
using System.Data;
using Backend.Game;
using Backend.Game.Services;
using Login.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServerSDK.Services;

namespace Backend.Db
{
	/// <summary>
	/// Sets up database dependency injection contexts and any database interfaces that are required.
	/// Also will validate and except if any configuration is missing.
	/// </summary>
	public static class DbSetup
	{
		/// <summary>
		/// Sets up database for the game server. Will setup any specific database dependencies that have to be injected.
		/// </summary>
		public static void Setup(IServiceCollection services, IServerConfiguration config)
		{
			if (config.DbConnectionString == null)
			{
				throw new DataException("Database not configured. Please set 'SqlConnectionString' env var");
			}
			services.AddDbContext<PlayersContext>(o => o.UseNpgsql(config.DbConnectionString));
			services.AddSingleton<IServerMutex, PostgresMutex>();
		}
	}
}

