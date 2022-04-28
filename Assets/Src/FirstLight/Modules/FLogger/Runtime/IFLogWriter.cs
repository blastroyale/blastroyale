namespace FirstLight.FLogger
{
	/// <summary>
	/// Handles log output.
	/// </summary>
	internal interface IFLogWriter
	{
		/// <summary>
		/// Writes the log to some output, depending on the implementation.
		/// </summary>
		/// <param name="level">The level / severity of the log</param>
		/// <param name="log">The log message</param>
		void Write(FLogLevel level, string log);
	}
}