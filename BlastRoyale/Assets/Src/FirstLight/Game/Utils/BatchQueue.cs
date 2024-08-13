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
	public class BatchQueue<T>
	{
		private class QueueData
		{
			public Func<UniTask<T>> _taskFactory;
			public UniTaskCompletionSource<T> _completionSource;
		}

		public int BatchSize { get; set; } = 3;
		private Queue<QueueData> _queue = new ();
		private UniTask _task;
		private int _running = 0;

		public BatchQueue(int size)
		{
			BatchSize = size;
		}

		public void Add(Func<UniTask<T>> item)
		{
			AddWrapped(new QueueData()
			{
				_taskFactory = item,
				_completionSource = null,
			});
		}

		public UniTask<T> AddAsync(Func<UniTask<T>> item)
		{
			var completion = new UniTaskCompletionSource<T>();
			AddWrapped(new QueueData()
			{
				_taskFactory = item,
				_completionSource = completion
			});

			return completion.Task;
		}

		private void AddWrapped(QueueData data)
		{
			if (_running < BatchSize)
			{
				Run(data).Forget();
			}
			else
			{
				_queue.Enqueue(data);
			}
		}

		private async UniTaskVoid Run(QueueData item)
		{
			_running++;
			var source = item._completionSource;
			try
			{
				var value = await item._taskFactory();
				source?.TrySetResult(value);
			}
			catch (OperationCanceledException ex)
			{
				source?.TrySetCanceled(ex.CancellationToken);
			}
			catch (Exception ex)
			{
				source?.TrySetException(ex);
			}
			finally
			{
				_running--;
				if (_queue.TryDequeue(out var i))
				{
					Run(i).Forget();
				}
			}
		}
	}
}