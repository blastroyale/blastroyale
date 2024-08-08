using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PlayFab.Internal;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	public class UniTaskUtils
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
	}
}