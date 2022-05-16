using System;

namespace Backend.Game;


/// <summary>
/// Interface for a distributed lock mechanism for the game server.
/// </summary>
public interface IServerMutex : IDisposable
{
	/// <summary>
	/// Locks a given user from being modified. This should be a pessimistic mutex.
	/// </summary>
	void Lock(string userId);

	/// <summary>
	/// Unlocks the user for modifications. Releases the mutex.
	/// </summary>
	void Unlock(string userId);
}