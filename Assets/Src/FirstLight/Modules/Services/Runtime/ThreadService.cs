using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace FirstLight.Services
{
	/// <summary>
	/// Helps with executing threaded tasks / Unity main thread tasks.
	/// </summary>
	public interface IThreadService
	{
		/// <summary>
		/// An Action dispatcher for the main Unity thread.
		/// </summary>
		public MainThreadDispatcher MainThreadDispatcher { get; }

		/// <summary>
		/// Runs <paramref name="threadedCall"/> in a background thread, and
		/// returns the resulting object back to the main Unity thread via the optional
		/// <paramref name="mainThreadCall"/>. Any exceptions during <paramref name="threadedCall"/>
		/// are reported via the <paramref name="error"/> callback.
		/// </summary>
		int Enqueue<T>(Func<T> threadedCall, Action<T> mainThreadCall = null, Action<Exception> error = null);

		/// <summary>
		/// The same as <see cref="Enqueue{T}"/>, but with a delay before executing <paramref name="threadedCall"/>.
		/// </summary>
		int EnqueueDelayed<T>(int delay, Func<T> threadedCall, Action<T> mainThreadCall = null,
		                      Action<Exception> error = null);

		/// <summary>
		/// Cancels a "run task". Note that this does not kill the running thread,
		/// but does prevent any callbacks that haven't been triggered yet from being
		/// triggered.
		/// </summary>
		void Cancel(int handle);
	}

	/// <inheritdoc />
	public class ThreadService : IThreadService
	{
		private const int MaxThreads = 3;
		private const int MinThreads = 1;

		private int _currentHandle;
		private readonly HashSet<int> _handles = new();

		public MainThreadDispatcher MainThreadDispatcher { get; }

		public ThreadService()
		{
			MainThreadDispatcher = new GameObject("MainThreadDispatcher").AddComponent<MainThreadDispatcher>();
			ThreadPool.SetMaxThreads(MaxThreads, MaxThreads);
			ThreadPool.SetMinThreads(MinThreads, MinThreads);
		}

		/// <inheritdoc />
		public int Enqueue<T>(Func<T> threadedCall, Action<T> mainThreadCall = null, Action<Exception> error = null)
		{
			return EnqueueDelayed(0, threadedCall, mainThreadCall, error);
		}

		/// <inheritdoc />
		public int EnqueueDelayed<T>(int delay, Func<T> threadedCall, Action<T> mainThreadCall = null,
		                             Action<Exception> error = null)

		{
			lock (_handles)
			{
				var handle = _currentHandle++;
				_handles.Add(handle);

				ThreadPool.QueueUserWorkItem(_ =>
				{
					try
					{
						if (!CanRun(handle)) return;
						if (delay > 0) Thread.Sleep(delay);
						if (!CanRun(handle)) return;

						var t = threadedCall();

						if (!CanRun(handle)) return;

						if (mainThreadCall != null)
						{
							MainThreadDispatcher.Enqueue(() =>
							{
								if (!CanRun(handle)) return;

								mainThreadCall(t);
							});
						}
					}
					catch (Exception e)
					{
						Cancel(handle);
						error?.Invoke(e);
					}
				});
				return handle;
			}
		}

		/// <inheritdoc />
		public void Cancel(int handle)
		{
			lock (_handles)
			{
				_handles.Remove(handle);
			}
		}

		private bool CanRun(int handle)
		{
			lock (_handles)
			{
				return _handles.Contains(handle);
			}
		}
	}

	/// <summary>
	/// Helps with running actions on the main Unity thread (e.g. from a background thread).
	/// </summary>
	public class MainThreadDispatcher : MonoBehaviour
	{
		private static readonly Queue<Action> _executionQueue = new();

		public void Update()
		{
			lock (_executionQueue)
			{
				while (_executionQueue.Count > 0)
				{
					_executionQueue.Dequeue().Invoke();
				}
			}
		}

		/// <summary>
		/// Adds an <paramref name="action"/> to be executed on the main Unity thread.
		/// </summary>
		public void Enqueue(Action action)
		{
			lock (_executionQueue)
			{
				_executionQueue.Enqueue(action);
			}
		}
	}
}