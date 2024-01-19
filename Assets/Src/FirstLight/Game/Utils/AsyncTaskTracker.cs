using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	public class AsyncTaskTracker
	{
		private HashSet<UniTask> _tracked = new ();
		private HashSet<UniTask> _pending = new();
		private HashSet<UniTask> _completed = new();

		public int Pending => _pending.Count;
		public int Completed => _completed.Count;

		public async UniTask WaitForCompletion()
		{
			await UniTask.WhenAll(_tracked);
		}

		private async UniTask TaskCompletionTracker(UniTask t)
		{
			if (t.Status is not UniTaskStatus.Succeeded)
				return;
			_pending.Add(t);
			await t;
			_pending.Remove(t);
			_completed.Add(t);
		}
		
		public void Add(params UniTask [] tasks)
		{
			foreach(var task in tasks)
			{
				_tracked.Add(task);
				_ = TaskCompletionTracker(task);
			}
		}

		private async UniTask OnCompleteAllTask(Action a)
		{
			while (_pending.Count > 0) await UniTask.Delay(10);
			a();
		}

		public void OnCompleteAll(Action a)
		{
			_ = OnCompleteAllTask(a);
		}
	}
}