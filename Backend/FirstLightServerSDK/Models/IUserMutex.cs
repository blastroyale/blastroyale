using System;
using System.Threading.Tasks;

namespace FirstLight.Server.SDK.Models
{
	public interface IUserMutex
	{
		ValueTask<IAsyncDisposable> LockUser(string userId);
	}
}