using System;
using System.Diagnostics;

namespace Quantum
{
	public class StopwatchBlock : IDisposable
	{
		private Stopwatch _stopwatch;
		private string _blockName;

		public StopwatchBlock(string blockName)
		{
			_blockName = blockName;
			_stopwatch = new Stopwatch();
			_stopwatch.Start();
		}

		void IDisposable.Dispose()
		{
			_stopwatch.Stop();
			Log.Info($"{_blockName}: {_stopwatch.Elapsed.TotalMilliseconds} ms");
		}
	}
}