using System;

namespace Best.HTTP.Shared.Logger
{
    /// <summary>
    /// Provides an implementation of <see cref="ILogOutput"/> that writes log messages to the Unity Debug Console.
    /// </summary>
    public sealed class UnityOutput : ILogOutput
    {
        /// <summary>
        /// Gets a value indicating whether this log output accepts color codes.
        /// </summary>
        /// <remarks>
        /// This property returns <c>true</c> when running in the Unity Editor and <c>false</c> otherwise.
        /// </remarks>
        public bool AcceptColor { get; } = UnityEngine.Application.isEditor;

        /// <summary>
        /// Writes a log message to the Unity Debug Console based on the specified log level.
        /// </summary>
        /// <param name="level">The log level of the message.</param>
        /// <param name="logEntry">The log message to write.</param>
        public void Write(Loglevels level, string logEntry)
        {
            switch (level)
            {
                case Loglevels.All:
                case Loglevels.Information:
                    UnityEngine.Debug.Log(logEntry);
                    break;

                case Loglevels.Warning:
                    UnityEngine.Debug.LogWarning(logEntry);
                    break;

                case Loglevels.Error:
                case Loglevels.Exception:
                    UnityEngine.Debug.LogError(logEntry);
                    break;
            }
        }

        /// <summary>
        /// This implementation does nothing.
        /// </summary>
        void ILogOutput.Flush() {}

        void IDisposable.Dispose() => GC.SuppressFinalize(this);
    }
}
