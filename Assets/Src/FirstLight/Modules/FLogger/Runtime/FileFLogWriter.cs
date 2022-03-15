using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace FirstLight.FLogger
{
	/// <summary>
	/// Outputs the logs to a file.
	/// </summary>
	internal class FileFLogWriter : IFLogWriter
	{
		private const string LogPathsKey = "FLogger.LogPaths";
		private const int LogFilesToKeep = 10;
		private const int MaxLogFileSize = 1000000; // In bytes
		private const int MaxLogFileAge = 1; // In hours
		private string LogsFolder = Path.Combine(Application.persistentDataPath, "Logs");

		private StreamWriter _streamWriter;

		private string _logPath;
		private List<string> _logPaths;

		private void Init()
		{
			var pathsString = PlayerPrefs.GetString(LogPathsKey);
			_logPaths = string.IsNullOrEmpty(pathsString) ? new List<string>() : pathsString.Split(';').ToList();
			_logPath = _logPaths.Count == 0 ? null : _logPaths[^1];

			// Create new log file if current one is too big or too old
			var logInfo = _logPath == null ? null : new FileInfo(_logPath);
			if (logInfo == null || logInfo.Length > MaxLogFileSize ||
			    logInfo.LastWriteTime.AddHours(MaxLogFileAge) < DateTime.Now)
			{
				if (!Directory.Exists(LogsFolder))
				{
					Directory.CreateDirectory(LogsFolder);
				}

				var logFileName = $"BlastRoyale_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.log";
				_logPath = Path.Combine(LogsFolder, logFileName);
			}
			else
			{
				_logPath = _logPaths[^1];
			}

			// Remove oldest log if we have more than the maximum allowed
			_logPaths.Add(_logPath);
			if (_logPaths.Count > LogFilesToKeep)
			{
				var oldestLog = _logPaths[0];
				File.Delete(oldestLog);
				_logPaths.RemoveAt(0);
			}

			PlayerPrefs.SetString(LogPathsKey, string.Join(';', _logPaths));

			_streamWriter = new StreamWriter(_logPath, true) {AutoFlush = true};
			_streamWriter.WriteLine("~~~~~~~~~ NEW SESSION ~~~~~~~~~");
		}

		/// <inheritdoc />
		public void Write(FLogLevel level, string log)
		{
			lock (this)
			{
				if (_streamWriter == null)
				{
					Init();
				}

				_streamWriter!.WriteLine(log);
			}
		}
	}
}