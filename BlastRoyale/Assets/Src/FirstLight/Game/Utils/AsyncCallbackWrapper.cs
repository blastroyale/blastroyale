using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

namespace FirstLight.Game.Utils
{
	
	/// <summary>
	/// This is a copy of <see cref="AsyncUnityEventHandler{T}"/>
	/// </summary>
	public class AsyncCallbackWrapper : IUniTaskSource, IDisposable
	{
		static Action<object> cancellationCallback = CancellationCallback;

		CancellationToken cancellationToken;
		CancellationTokenRegistration registration;
		bool isDisposed;
		bool callOnce = true;
		UniTaskCompletionSourceCore<AsyncUnit> core;

		public AsyncCallbackWrapper(CancellationToken cancellationToken = default)
		{
			this.cancellationToken = cancellationToken;
			if (cancellationToken.IsCancellationRequested)
			{
				isDisposed = true;
			}
		}

		public UniTask OnInvokeAsync()
		{
			core.Reset();
			if (isDisposed)
			{
				core.TrySetCanceled(cancellationToken);
			}

			return new UniTask(this, core.Version);
		}

		public void Invoke()
		{
			core.TrySetResult(AsyncUnit.Default);
		}

		static void CancellationCallback(object state)
		{
			var self = (AsyncUnityEventHandler) state;
			self.Dispose();
		}

		public void Dispose()
		{
			if (!isDisposed)
			{
				isDisposed = true;
				core.TrySetCanceled(cancellationToken);
			}
		}

		void IUniTaskSource.GetResult(short token)
		{
			try
			{
				core.GetResult(token);
			}
			finally
			{
				if (callOnce)
				{
					Dispose();
				}
			}
		}

		UniTaskStatus IUniTaskSource.GetStatus(short token)
		{
			return core.GetStatus(token);
		}

		UniTaskStatus IUniTaskSource.UnsafeGetStatus()
		{
			return core.UnsafeGetStatus();
		}

		void IUniTaskSource.OnCompleted(Action<object> continuation, object state, short token)
		{
			core.OnCompleted(continuation, state, token);
		}
	}
}