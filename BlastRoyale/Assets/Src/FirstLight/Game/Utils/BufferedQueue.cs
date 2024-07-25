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
		public bool OnlyKeepLast { get; set; }
		
		private Queue<Action> _queue = new ();
		private UniTask _task;
		
		public void Add(Action item)
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
				FLog.Verbose("Ticking q "+item);
				item();
				await UniTask.Delay(BufferTime);
			}
		}
	}
}