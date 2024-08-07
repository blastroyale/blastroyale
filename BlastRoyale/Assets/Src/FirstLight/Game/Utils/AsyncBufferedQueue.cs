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

		public void Clear() => _queue.Clear();
		
		public void Add(Func<UniTask> item)
		{
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