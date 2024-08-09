using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Simple async Buffered Queue to buffer/stagger actions
	/// </summary>
	public class AsyncBufferedQueue
	{
		/// <summary>
		/// Minimum amount of time between updates
		/// </summary>
		public TimeSpan BufferTime = TimeSpan.FromSeconds(3);

		/// <summary>
		/// When true, will only use last item when buffering happens
		/// </summary>
		public bool OnlyKeepLast;

		public AsyncBufferedQueue(TimeSpan time, bool onlyLast = false)
		{
			OnlyKeepLast = onlyLast;
			BufferTime = time;
		}
		
		private Queue<Func<UniTask>> _queue = new ();
		private UniTask _task;
		private bool _locked;

		public void Clear() => _queue.Clear();
		
		public void Add(Func<UniTask> item)
		{
			if (_locked)
			{
				return;
			}
			if (OnlyKeepLast)
			{
				_queue.Clear();
			}
			_queue.Enqueue(item);
			if (_task.Status != UniTaskStatus.Pending)
			{
				_task = Task();
			}
		}

		/// <summary>
		/// Will run all pending tasks and block the async loop while it happens
		/// </summary>
		public async UniTask Dispose()
		{
			try
			{
				_locked = true;
				if (_task.Status != UniTaskStatus.Pending)
				{
					FLog.Verbose("Running remaining queue items");
					await Task();
				}
				else
				{
					FLog.Verbose("Awaiting existing query");
					await UniTask.WaitUntil(() => _task.Status != UniTaskStatus.Pending);
				}
			}
			finally
			{
				_locked = false;
			}
		}

		private async UniTask Task()
		{
			while (_queue.TryDequeue(out var item))
			{
				await item();
				await UniTask.Delay(BufferTime);
			}
		}
	}
}