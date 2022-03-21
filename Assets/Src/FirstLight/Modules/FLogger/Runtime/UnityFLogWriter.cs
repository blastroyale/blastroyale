using System;
using UnityEngine;

namespace FirstLight.FLogger
{
	/// <summary>
	/// Outputs the logs to the Unity console (Debug.Log*).
	/// </summary>
	internal class UnityFLogWriter : IFLogWriter
	{
		/// <inheritdoc />
		public void Write(FLogLevel level, string log)
		{
			switch (level)
			{
				case FLogLevel.Error:
					Debug.LogError(log);
					break;
				case FLogLevel.Warn:
					Debug.LogWarning(log);
					break;
				case FLogLevel.Info:
				case FLogLevel.Verbose:
					Debug.Log(log);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(level), level, null);
			}
		}
	}
}