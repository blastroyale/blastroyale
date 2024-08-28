using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FirstLight.FLogger
{
	/// <summary>
	/// The main logger that handles logging in the editor and
	/// on devices.
	/// </summary>
	[PublicAPI]
	public static class FLog
	{
		private static FLogFormatter _formatter;
		private static List<IFLogWriter> _writers;

		private static FileFLogWriter _fileWriter;

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
			if (_formatter != null) return;
			_formatter = new FLogFormatter();
			_formatter.SetMainThreadID();

			_writers = new List<IFLogWriter>(2)
			{
				new UnityFLogWriter()
			};

			_fileWriter = new FileFLogWriter();
			_fileWriter.Init();

			Application.logMessageReceivedThreaded += (log, stackTrace, type) =>
			{
				_fileWriter.Write(type.ToFLogLevel(), log);
			};
		}

		/// <summary>
		/// <inheritdoc cref="FLogLevel.Error"/>
		/// </summary>
		/// <param name="tag">An optional tag. If empty it uses the calling class name</param>
		/// <param name="message">The message of the log.</param>
		/// <param name="e">An optional exception that will be added to the log</param>
		[Conditional("LOG_LEVEL_VERBOSE")]
		[Conditional("LOG_LEVEL_INFO")]
		[Conditional("LOG_LEVEL_WARN")]
		[Conditional("LOG_LEVEL_ERROR")]
		[HideInCallstack]
		public static void Error(string tag, string message, Exception e = null) => WriteToAll(FLogLevel.Error, _formatter.FormatLog(FLogLevel.Error, tag, message, e));

		/// <summary>
		/// <inheritdoc cref="FLogLevel.Error"/>
		/// </summary>
		/// <param name="message">The message of the log.</param>
		/// <param name="e">An optional exception that will be added to the log</param>
		[Conditional("LOG_LEVEL_VERBOSE")]
		[Conditional("LOG_LEVEL_INFO")]
		[Conditional("LOG_LEVEL_WARN")]
		[Conditional("LOG_LEVEL_ERROR")]
		[HideInCallstack]
		public static void Error(string message, Exception e = null) => WriteToAll(FLogLevel.Error, _formatter.FormatLog(FLogLevel.Error, null, message, e));

		/// <summary>
		/// <inheritdoc cref="FLogLevel.Warn"/>
		/// </summary>
		/// <param name="tag">An optional tag. If empty it uses the calling class name</param>
		/// <param name="message">The message of the log.</param>
		/// <param name="e">An optional exception that will be added to the log</param>
		[Conditional("LOG_LEVEL_VERBOSE")]
		[Conditional("LOG_LEVEL_INFO")]
		[Conditional("LOG_LEVEL_WARN")]
		[HideInCallstack]
		public static void Warn(string tag, string message, Exception e = null) => WriteToAll(FLogLevel.Warn, _formatter.FormatLog(FLogLevel.Warn, tag, message, e));

		/// <summary>
		/// <inheritdoc cref="FLogLevel.Warn"/>
		/// </summary>
		/// <param name="message">The message of the log.</param>
		/// <param name="e">An optional exception that will be added to the log</param>
		[Conditional("LOG_LEVEL_VERBOSE")]
		[Conditional("LOG_LEVEL_INFO")]
		[Conditional("LOG_LEVEL_WARN")]
		[HideInCallstack]
		public static void Warn(string message, Exception e = null) => WriteToAll(FLogLevel.Warn, _formatter.FormatLog(FLogLevel.Warn, null, message, e));

		/// <summary>
		/// <inheritdoc cref="FLogLevel.Info"/>
		/// </summary>
		/// <param name="tag">An optional tag. If empty it uses the calling class name</param>
		/// <param name="message">The message of the log.</param>
		/// <param name="e">An optional exception that will be added to the log</param>
		[Conditional("LOG_LEVEL_VERBOSE")]
		[Conditional("LOG_LEVEL_INFO")]
		[HideInCallstack]
		public static void Info(string tag, string message, Exception e = null) => WriteToAll(FLogLevel.Info, _formatter.FormatLog(FLogLevel.Info, tag, message, e));

		/// <summary>
		/// <inheritdoc cref="FLogLevel.Info"/>
		/// </summary>
		/// <param name="message">The message of the log.</param>
		/// <param name="e">An optional exception that will be added to the log</param>
		[Conditional("LOG_LEVEL_VERBOSE")]
		[Conditional("LOG_LEVEL_INFO")]
		[HideInCallstack]
		public static void Info(string message, Exception e = null) => WriteToAll(FLogLevel.Info, _formatter.FormatLog(FLogLevel.Info, null, message, e));

		/// <summary>
		/// <inheritdoc cref="FLogLevel.Verbose"/>
		/// </summary>
		/// <param name="tag">An optional tag. If empty it uses the calling class name</param>
		/// <param name="message">The message of the log.</param>
		/// <param name="e">An optional exception that will be added to the log</param>
		[Conditional("LOG_LEVEL_VERBOSE")]
		[HideInCallstack]
		public static void Verbose(string tag, string message, Exception e = null) => WriteToAll(FLogLevel.Verbose, _formatter.FormatLog(FLogLevel.Verbose, tag, message, e));

		/// <summary>
		/// <inheritdoc cref="FLogLevel.Verbose"/>
		/// </summary>
		/// <param name="message">The message of the log.</param>
		/// <param name="e">An optional exception that will be added to the log</param>
		[Conditional("LOG_LEVEL_VERBOSE")]
		[HideInCallstack]
		public static void Verbose(string message, Exception e = null) => WriteToAll(FLogLevel.Verbose, _formatter.FormatLog(FLogLevel.Verbose, null, message, e));

		/// <summary>
		/// Verbose log that will only de-serialize the object if the log level is verbose.
		/// Will serialize via .ToString() method
		/// </summary>
		[Conditional("LOG_LEVEL_VERBOSE")]
		[HideInCallstack]
		public static void Verbose(object o) => WriteToAll(FLogLevel.Verbose, _formatter.FormatLog(FLogLevel.Verbose, o.GetType().Name, o.ToString()));

		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		[Conditional("LOG_LEVEL_VERBOSE")]
		public static void Spank() => WriteToAll(FLogLevel.Verbose, "Slap!!!");

		/// <summary>
		/// Returns the path of the current (latest) log file.
		/// </summary>
		public static string GetCurrentLogFilePath() => _fileWriter.CurrentLogPath;

		[HideInCallstack]
		private static void WriteToAll(FLogLevel level, string log)
		{
			foreach (var w in _writers)
			{
				w.Write(level, log);
			}
		}
	}
}