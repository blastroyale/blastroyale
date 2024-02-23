using System;
using System.Threading.Tasks;

namespace FirstLight.Server.SDK.Services
{
	/// <summary>
	/// Interface for a distributed lock mechanism for the game server.
	/// </summary>
	public interface IServerMutex : IDisposable
	{
		/// <summary>
		/// Locks a given user from being modified. This should be a pessimistic mutex.
		/// </summary>
		Task Lock(string userId);

		/// <summary>
		/// Unlocks the user for modifications. Releases the mutex.
		/// </summary>
		void Unlock(string userId);

		/// <summary>
		/// Runs a given function as a locked transaction for the given player
		/// </summary>
		Task<IServerMutex> Transaction(string userId, Func<Task> a);
	}
}


