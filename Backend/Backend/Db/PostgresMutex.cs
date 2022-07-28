using System;
using System.Collections.Generic;
using Backend.Db;
using Medallion.Threading.Postgres;
using ServerSDK.Services;

namespace Backend.Game;


/// <summary>
/// Implementation of a distributed lock using postgres.
/// </summary>
public class PostgresMutex : IServerMutex
{
	private Dictionary<string, PostgresDistributedLockHandle> _handles = new ();

	/// <inheritdoc />
	public void Lock(string userId)
	{
		var mutex = new PostgresDistributedLock(new PostgresAdvisoryLockKey(userId, allowHashing: true), DbSetup.ConnectionString);
		_handles[userId] = mutex.Acquire();
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