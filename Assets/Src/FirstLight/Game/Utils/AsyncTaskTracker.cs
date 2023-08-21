using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	public class AsyncTaskTracker
	{
		private Dictionary<int, Task> _tracked = new ();
		private HashSet<int> _pending = new();
		private HashSet<int> _completed = new();

		public int Pending => _pending.Count;
		public int Completed => _completed.Count;

		public async Task WaitForCompletion()
		{
			await Task.WhenAll(_tracked.Values);
		}

		private async Task TaskCompletionTracker(Task t)
		{
			if (t == null || t.IsCanceled || t.IsCompleted || t.IsCompletedSuccessfully || t.IsFaulted)
				return;
			_pending.Add(t.Id);
			await t;
			_pending.Remove(t.Id);
			_completed.Add(t.Id);
		}
		
		public void Add(params Task [] tasks)
		{
			foreach(var task in tasks)
			{
				_tracked.Add(task.Id, task);
				_ = TaskCompletionTracker(task);
			}
		}

		private async Task OnCompleteAllTask(Action a)
		{
			while (_pending.Count > 0) await Task.Delay(10);
			a();
		}

		public void OnCompleteAll(Action a)
		{
			_ = OnCompleteAllTask(a);
		}
	}
}