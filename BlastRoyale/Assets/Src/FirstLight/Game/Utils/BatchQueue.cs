using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Simple async batching mechanism
	/// </summary>
	public class BatchQueue
	{
		public int BatchSize { get; set; } = 3;
		private Queue<Func<UniTask>> _queue = new ();
		private UniTask _task;
		private int _running = 0;
		public void Add(Func<UniTask> item)
		{
			if (_running < BatchSize)
			{
				Run(item);
			}
			else
			{
				_queue.Enqueue(item);
			}
		}

		private void Run(Func<UniTask> item)
		{
			_running++;
			item().ContinueWith(OnComplete).Forget();
		}

		private void OnComplete()
		{
			_running--;
			if (_queue.TryDequeue(out var i))
			{
				Run(i);
			}
		}
	}
}