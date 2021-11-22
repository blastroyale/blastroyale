// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	/// <summary>
	/// Helper class for the statechart to work properly
	/// </summary>
	internal static class StatechartUtils
	{
		/// <summary>
		/// Cleans ups the given <paramref name="stackTrace"/> of unnecessary string data
		/// </summary>
		public static string RemoveGarbageFromStackTrace(string stackTrace)
		{
			var lineIdx = stackTrace.IndexOf('\n') + 1;
			do
			{
				stackTrace = stackTrace.Remove(0, lineIdx);
				lineIdx = stackTrace.IndexOf('\n') + 1;
			}
			while (stackTrace.Substring(0, lineIdx).Contains("StateMachine.Internal"));

			return stackTrace + "\n";
		}
	}
}
