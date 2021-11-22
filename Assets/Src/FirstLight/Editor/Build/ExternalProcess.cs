using System;
using System.Diagnostics;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Used to run processes that would otherwise be used in the terminal.
	/// </summary>
	public abstract class ExternalProcess : IDisposable
	{
		protected readonly Process Process;
		public int ExitCode => Process.ExitCode;

		protected ExternalProcess(string workingDir, string pathToBinary)
		{
			var startInfo = new ProcessStartInfo
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
				FileName = pathToBinary,
				CreateNoWindow = true,
				WorkingDirectory = workingDir
			};

			Process = new Process { StartInfo = startInfo };
		}

		/// <summary>
		/// Execute a command eg. "status --verbose"
		/// </summary>
		protected string ExecuteCommand(string args)
		{
			Process.StartInfo.Arguments = args;
			Process.Start();
			var output = Process.StandardOutput.ReadToEnd().Trim();
			Process.WaitForExit();
			return output;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			Process?.Dispose();
		}
	}
}