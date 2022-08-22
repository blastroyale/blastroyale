using System.Collections.Generic;
using System.Threading.Tasks;
using ServerSDK.Services;

namespace Tests.Stubs;

/// <summary>
/// In memory rudimentary hacky wacky implementation of async single threaded server mutex
/// </summary>
public class InMemoryMutex : IServerMutex
{
	private Task? _task;
	private HashSet<string> _locked = new ();
	
	private async Task WaitForever() => await Task.Delay(int.MaxValue);
	
	public void Dispose()
	{
		_locked.Clear();
		_task = null;
	}

	public async Task Lock(string userId)
	{
		if (_locked.Contains(userId))
			_task?.Wait();
		_locked.Add(userId);
		_task = WaitForever();
	}

	public void Unlock(string userId)
	{
		_locked.Remove(userId);
		_task = null;
	}
}