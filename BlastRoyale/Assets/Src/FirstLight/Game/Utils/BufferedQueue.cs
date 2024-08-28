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
	public class BufferedQueue
	{
		/// <summary>
		/// Minimum amount of time between updates
		/// </summary>
		public TimeSpan BufferTime = TimeSpan.FromSeconds(3);

		/// <summary>
		/// When true, will only use last item when buffering happens
		/// </summary>
		public bool OnlyKeepLast;

		public BufferedQueue(TimeSpan time, bool onlyLast = false)
		{
			BufferTime = time;
			OnlyKeepLast = onlyLast;
		}
		
		private Queue<Action> _queue = new ();
		private UniTask _task;
		
		public void Add(Action item)
		{
			FLog.Verbose("Added queue item "+item.Method.Name+" "+OnlyKeepLast);
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
				FLog.Verbose("Ticking queue item "+item.Method.Name);
				item();
				await UniTask.Delay(BufferTime);
			}
		}
	}
}