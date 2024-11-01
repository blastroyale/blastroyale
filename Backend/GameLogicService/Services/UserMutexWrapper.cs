using System;
using System.Threading.Tasks;
using FirstLight.Server.SDK.Models;
using FirstLightServerSDK.Modules;
using Medallion.Threading;

namespace GameLogicService.Services;

/// <summary>
/// This is a wrapper for the mutex so we don't have to pass all distributed mutex dependencies to the game
/// </summary>
public class UserMutexWrapper : IUserMutex
{
	private readonly IDistributedLockProvider _provider;

	public UserMutexWrapper(IDistributedLockProvider provider)
	{
		_provider = provider;
	}

	public async ValueTask<IAsyncDisposable> LockUser(string userId)
	{
		return await _provider.LockUser(userId);
	}
}