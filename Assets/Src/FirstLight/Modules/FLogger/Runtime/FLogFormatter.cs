using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using UnityEngine;

namespace FirstLight.FLogger
{
	/// <summary>
	/// A helper class that formats the log string and adds data to it.
	/// </summary>
	internal class FLogFormatter
	{
		private const int MaxLength = 16384;

		private int _mainThreadID = -1;

		/// <summary>
		/// Formats the log message with additional data (e.g. current frame, current
		/// thread, time, log level...).
		/// This will also strip the log if it's longer than <see cref="MaxLength"/>.
		/// </summary>
		/// <param name="level">The level / severity of the log</param>
		/// <param name="tag">An optional tag. If empty it uses the calling class name</param>
		/// <param name="message">The message of the log.</param>
		/// <param name="e">An optional exception that will be added to the log</param>
		/// <returns>The formatted log string</returns>
		public string FormatLog(FLogLevel level, string tag, string message, Exception e = null)
		{
			var sb = new StringBuilder(512);

#if !UNITY_EDITOR
            // Add log level
            sb.AppendFormat("{0,-8}", level);
			
			// Add date & time
			sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
#endif

			// Add thread ID
			sb.Append(" (T");
			sb.Append(_mainThreadID);

			// Add frame count
			if (Thread.CurrentThread.ManagedThreadId == _mainThreadID)
			{
				sb.Append(" F");
				sb.Append(Time.frameCount);
			}

			sb.Append(") ");

			// Add tag (if missing use name of calling class)
			tag ??= GetCallingClass();
			sb.Append(" [");
			sb.Append(tag);
			sb.Append("] ");

			// Add the body of the log
			sb.Append(message);

			// Add stack trace
			if (e != null)
			{
				sb.Append(e);
			}

			string log = sb.ToString();

			// Strip log if it's too long
			if (log.Length > MaxLength)
			{
				log = log[..MaxLength];
			}

			return log;
		}

		private static string GetCallingClass()
		{
			var stackFrame = new StackFrame(3, false);

			var method = stackFrame.GetMethod();
			if (method == null)
			{
				return null;
			}

			var declaringType = method.DeclaringType;
			if (declaringType == null)
			{
				return null;
			}

			var reflectedType = declaringType.ReflectedType;
			if (reflectedType == null)
			{
				return declaringType.Name;
			}

			var innerType = reflectedType.DeclaringType;
			if (innerType == null)
			{
				return reflectedType.Name;
			}

			while (innerType.DeclaringType != null)
			{
				innerType = innerType.DeclaringType;
			}

			return innerType.Name;
		}

		/// <summary>
		/// Has to be called from the main thread, and will allow reporting frame count while in the Unity thread.
		/// </summary>
		public void SetMainThreadID() => _mainThreadID = Thread.CurrentThread.ManagedThreadId;
	}
}