using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PlayFab.Internal;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	public static class UniTaskUtils
	{
		public static async UniTask<bool> WaitUntilTimeout(Func<bool> cond, TimeSpan timeout)
		{
			var cts = new CancellationTokenSource();
			cts.CancelAfterSlim(timeout);
			try
			{
				await UniTask.WaitUntil(cond, cancellationToken: cts.Token);
			}
			catch (OperationCanceledException e)
			{
				Debug.LogWarning(e);
				return false;
			}

			return true;
		}

		private class IDisposableWrapper : IDisposable
		{
			public SemaphoreSlim Semaphore;

			public void Dispose()
			{
				Semaphore.Release();
			}
		}

		public static async UniTask<IDisposable> AcquireAsync(this SemaphoreSlim semaphoreSlim)
		{
			await semaphoreSlim.WaitAsync();
			return new IDisposableWrapper() {Semaphore = semaphoreSlim};
		}
	}
}