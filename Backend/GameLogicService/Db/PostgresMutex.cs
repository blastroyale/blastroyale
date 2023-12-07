using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Db;
using Backend.Game.Services;
using Medallion.Threading.Postgres;
using FirstLight.Server.SDK.Services;
using Microsoft.Extensions.Logging;

namespace Backend.Game
{
	/// <summary>
	/// Implementation of a distributed lock using postgres.
	/// </summary>
	public class PostgresMutex : IServerMutex
	{
		private Dictionary<string, PostgresDistributedLockHandle> _handles = new ();
		private IBaseServiceConfiguration _cfg;
		private ILogger _log;
		
		public PostgresMutex(IBaseServiceConfiguration cfg, ILogger log)
		{
			_cfg = cfg;
			_log = log;
		}

		public async Task<IServerMutex> Transaction(string userId, Func<Task> a)
		{
			try
			{
				await Lock(userId);
				await a();
			}
			finally
			{
				Unlock(userId);
			}
			return this;
		}
		
		/// <inheritdoc />
		public async Task Lock(string userId)
		{
			var mutex = new PostgresDistributedLock(new PostgresAdvisoryLockKey(userId, allowHashing: true), _cfg.DbConnectionString);
			_handles[userId] = await mutex.AcquireAsync(timeout: TimeSpan.FromSeconds(10));
		}

		/// <inheritdoc />
		public void Unlock(string userId)
		{
			_handles[userId].Dispose();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			foreach (var l in _handles.Values)
			{
				l.Dispose();
			}
			_handles.Clear();
		}
	}
	
	public class NoMutex : IServerMutex
	{
		public void Dispose() {}
		public async Task Lock(string userId) {}
		public void Unlock(string userId) {}
		public Task<IServerMutex> Transaction(string userId, Func<Task> a)
		{
			return null;
		}
	}
}


