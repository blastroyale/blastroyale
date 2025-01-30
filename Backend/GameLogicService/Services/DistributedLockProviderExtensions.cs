using System.Threading.Tasks;
using Medallion.Threading;

namespace FirstLightServerSDK.Modules
{
	public static class DistributedLockProviderExtensions
	{
		private const string USER_LOCK_PREFIX = "user_";

		public static ValueTask<IDistributedSynchronizationHandle> LockUser(
			this IDistributedLockProvider provider, string userId)
		{
			return provider.AcquireLockAsync(USER_LOCK_PREFIX + userId);
		}
	}
}