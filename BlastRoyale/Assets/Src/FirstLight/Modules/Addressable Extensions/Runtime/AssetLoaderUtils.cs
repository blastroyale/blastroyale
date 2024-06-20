using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace FirstLight.AddressablesExtensions
{
	/// <summary>
	/// Helper class with util improvements for loading methods
	/// </summary>
	public static class AssetLoaderUtils
	{
		/// <summary>
		/// Helper method to interpolate over a list of the given <paramref name="tasks"/>.
		/// Returns the first completed task and moves to the next until all tasks are completed.
		/// </summary>
		/// <remarks>
		/// Based on the implementation of a .Net engineer <seealso cref="https://devblogs.microsoft.com/pfxteam/processing-tasks-as-they-complete/"/>
		/// </remarks>
		public static Task<Task<T>>[] Interleaved<T>(IEnumerable<Task<T>> tasks)
		{
			var inputTasks = tasks.ToList();
			var buckets = new TaskCompletionSource<Task<T>>[inputTasks.Count];
			var results = new Task<Task<T>>[buckets.Length];
			var nextTaskIndex = -1;
			
			for (var i = 0; i < buckets.Length; i++) 
			{
				buckets[i] = new TaskCompletionSource<Task<T>>();
				results[i] = buckets[i].Task;
			}
			
			foreach (var inputTask in inputTasks)
			{
				inputTask.ContinueWith(Continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
			}

			return results;

			// Local function
			void Continuation(Task<T> completed)
			{
				buckets[Interlocked.Increment(ref nextTaskIndex)].TrySetResult(completed);
			}
		}
	}
}