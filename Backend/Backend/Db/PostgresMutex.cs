using System;
using Backend.Db;
using Medallion.Threading.Postgres;

namespace Backend.Game;


/// <summary>
/// Implementation of a distributed lock using postgres.
/// </summary>
public class PostgresMutex : IServerMutex
{
	private PostgresDistributedLockHandle _handle;

	/// <inheritdoc />
	public void Lock(string userId)
	{
		var mutex = new PostgresDistributedLock(new PostgresAdvisoryLockKey(userId, allowHashing: true), DbSetup.ConnectionString);
		_handle = mutex.Acquire();
	}

	/// <inheritdoc />
	public void Unlock(string userId)
	{
		_handle?.Dispose();
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_handle?.Dispose();
	}
}