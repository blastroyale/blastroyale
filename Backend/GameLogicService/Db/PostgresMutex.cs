using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Db;
using Backend.Game.Services;
using Medallion.Threading.Postgres;
using ServerSDK.Services;

namespace Backend.Game
{
	/// <summary>
	/// Implementation of a distributed lock using postgres.
	/// </summary>
	public class PostgresMutex : IServerMutex
	{
		private Dictionary<string, PostgresDistributedLockHandle> _handles = new ();
		private IServerConfiguration _cfg;
		
		public PostgresMutex(IServerConfiguration cfg)
		{
			_cfg = cfg;
		}
		
		/// <inheritdoc />
		public async Task Lock(string userId)
		{
			var mutex = new PostgresDistributedLock(new PostgresAdvisoryLockKey(userId, allowHashing: true), _cfg.DbConnectionString);
			_handles[userId] = await mutex.AcquireAsync();
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
}


