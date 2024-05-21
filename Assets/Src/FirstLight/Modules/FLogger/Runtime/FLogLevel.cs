using System;
using UnityEngine;

namespace FirstLight.FLogger
{
	/// <summary>
	/// Describes different levels of log information that FLogger supports.
	/// </summary>
	internal enum FLogLevel
	{
		/// <summary>
		/// Unexpected errors and failures.
		/// </summary>
		Error,

		/// <summary>
		/// Abnormal situations that may result in problems.
		/// </summary>
		Warn,

		/// <summary>
		/// High-level informational messages.
		/// </summary>
		Info,

		/// <summary>
		/// Detailed informational messages.
		/// </summary>
		Verbose
	}

	internal static class FLogLevelExtensions
	{
		public static FLogLevel ToFLogLevel(this LogType logType)
		{
			return logType switch
			{
				LogType.Error     => FLogLevel.Error,
				LogType.Assert    => FLogLevel.Error,
				LogType.Warning   => FLogLevel.Warn,
				LogType.Log       => FLogLevel.Info,
				LogType.Exception => FLogLevel.Error,
				_                 => throw new ArgumentOutOfRangeException(nameof(logType), logType, null)
			};
		}
	}
}