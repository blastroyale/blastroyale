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
}