using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FirstLight.FLogger
{
	/// <summary>
	/// The main logger that handles logging in the editor and
	/// on devices.
	/// </summary>
	public static class FLog
	{
		private static FLogFormatter _formatter;
		private static List<IFLogWriter> _writers;

		/// <summary>
		/// Initializes the logger with the default writers (Unity and File),
		/// and sets the main thread ID.
		///
		/// Note: This must be called from the main Unity thread.
		/// </summary>
		[Conditional("LOG_LEVEL_VERBOSE")]
		[Conditional("LOG_LEVEL_INFO")]
		[Conditional("LOG_LEVEL_WARN")]
		[Conditional("LOG_LEVEL_ERROR")]
		public static void Init()
		{
			_formatter = new FLogFormatter();
			_formatter.SetMainThreadID();

			_writers = new List<IFLogWriter>(2)
			{
				new UnityFLogWriter(),
				new FileFLogWriter()
			};
		}

		/// <summary>
		/// <inheritdoc cref="FLogLevel.Error"/>
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="message"></param>
		/// <param name="e">An optional exception that will be added to the log</param>
		[Conditional("LOG_LEVEL_VERBOSE")]
		[Conditional("LOG_LEVEL_INFO")]
		[Conditional("LOG_LEVEL_WARN")]
		[Conditional("LOG_LEVEL_ERROR")]
		public static void Error(string tag, string message, Exception e = null) =>
			WriteToAll(FLogLevel.Error, _formatter.FormatLog(FLogLevel.Error, tag, message, e));

		/// <summary>
		/// <inheritdoc cref="FLogLevel.Warn"/>
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="message"></param>
		/// <param name="e">An optional exception that will be added to the log</param>
		[Conditional("LOG_LEVEL_VERBOSE")]
		[Conditional("LOG_LEVEL_INFO")]
		[Conditional("LOG_LEVEL_WARN")]
		public static void Warn(string tag, string message, Exception e = null) =>
			WriteToAll(FLogLevel.Warn, _formatter.FormatLog(FLogLevel.Warn, tag, message, e));

		/// <summary>
		/// <inheritdoc cref="FLogLevel.Info"/>
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="message"></param>
		/// <param name="e">An optional exception that will be added to the log</param>
		[Conditional("LOG_LEVEL_VERBOSE")]
		[Conditional("LOG_LEVEL_INFO")]
		public static void Info(string tag, string message, Exception e = null) =>
			WriteToAll(FLogLevel.Info, _formatter.FormatLog(FLogLevel.Info, tag, message, e));

		/// <summary>
		/// <inheritdoc cref="FLogLevel.Verbose"/>
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="message"></param>
		/// <param name="e">An optional exception that will be added to the log</param>
		[Conditional("LOG_LEVEL_VERBOSE")]
		public static void Verbose(string tag, string message, Exception e = null) =>
			WriteToAll(FLogLevel.Verbose, _formatter.FormatLog(FLogLevel.Verbose, tag, message, e));

		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		[Conditional("LOG_LEVEL_VERBOSE")]
		public static void Spank() => WriteToAll(FLogLevel.Verbose, "Slap!!!");

		private static void WriteToAll(FLogLevel level, string log)
		{
			foreach (var w in _writers)
			{
				w.Write(level, log);
			}
		}
	}
}