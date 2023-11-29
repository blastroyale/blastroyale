using System;

namespace Best.HTTP.Shared.Logger
{
    [HideFromDocumentation]
    public sealed class HideFromDocumentation : Attribute
    {

    }

    /// <summary>
    /// Available logging levels.
    /// </summary>
    public enum Loglevels : int
    {
        /// <summary>
        /// All message will be logged.
        /// </summary>
        All,

        /// <summary>
        /// Only Informations and above will be logged.
        /// </summary>
        Information,

        /// <summary>
        /// Only Warnings and above will be logged.
        /// </summary>
        Warning,

        /// <summary>
        /// Only Errors and above will be logged.
        /// </summary>
        Error,

        /// <summary>
        /// Only Exceptions will be logged.
        /// </summary>
        Exception,

        /// <summary>
        /// No logging will occur.
        /// </summary>
        None
    }

    /// <summary>
    /// Represents an output target for log messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface defines methods for writing log messages to an output target.
    /// Implementations of this interface are used to configure where log messages
    /// should be written.
    /// </para>
    /// <para>
    /// Two of its out-of-the-box implementations are
    /// <list type="bullet">
    ///     <item><description><see cref="UnityOutput">UnityOutput</see></description></item>
    ///     <item><description><see cref="FileOutput">FileOutput</see></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ILogOutput : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the log output supports colored text.
        /// </summary>
        bool AcceptColor { get; }

        /// <summary>
        /// Writes a log entry to the output.
        /// </summary>
        /// <param name="level">The logging level of the entry.</param>
        /// <param name="logEntry">The log message to write.</param>
        void Write(Loglevels level, string logEntry);

        /// <summary>
        /// Flushes any buffered log entries to the output.
        /// </summary>
        void Flush();
    }

    /// <summary>
    /// Represents a logger for recording log messages.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Gets or sets the minimum severity level for logging.
        /// </summary>
        Loglevels Level { get; set; }

        /// <summary>
        /// Gets or sets the output target for log messages.
        /// </summary>
        /// <value>
        /// The <see cref="ILogOutput"/> instance used to write log messages.
        /// </value>
        ILogOutput Output { get; set; }

        /// <summary>
        /// Gets a value indicating whether diagnostic logging is enabled.
        /// </summary>
        /// <remarks>
        /// Diagnostic logging is enabled when <see cref="Level"/> is set to <see cref="Loglevels.All"/>.
        /// </remarks>
        bool IsDiagnostic { get; }

        /// <summary>
        /// Logs a message with <see cref="Loglevels.All"/> level.
        /// </summary>
        /// <param name="division">The division or category of the log message.</param>
        /// <param name="msg">The verbose log message.</param>
        /// <param name="context">The optional <see cref="LoggingContext"/> for additional context.</param>
        void Verbose(string division, string msg, LoggingContext context = null);

        /// <summary>
        /// Logs a message with <see cref="Loglevels.Information"/> level.
        /// </summary>
        /// <param name="division">The division or category of the log message.</param>
        /// <param name="msg">The verbose log message.</param>
        /// <param name="context">The optional <see cref="LoggingContext"/> for additional context.</param>
        void Information(string division, string msg, LoggingContext context = null);

        /// <summary>
        /// Logs a message with <see cref="Loglevels.Warning"/> level.
        /// </summary>
        /// <param name="division">The division or category of the log message.</param>
        /// <param name="msg">The verbose log message.</param>
        /// <param name="context">The optional <see cref="LoggingContext"/> for additional context.</param>
        void Warning(string division, string msg, LoggingContext context = null);

        /// <summary>
        /// Logs a message with <see cref="Loglevels.Error"/> level.
        /// </summary>
        /// <param name="division">The division or category of the log message.</param>
        /// <param name="msg">The verbose log message.</param>
        /// <param name="context">The optional <see cref="LoggingContext"/> for additional context.</param>
        void Error(string division, string msg, LoggingContext context = null);

        /// <summary>
        /// Logs a message with <see cref="Loglevels.Exception"/> level.
        /// </summary>
        /// <param name="division">The division or category of the log message.</param>
        /// <param name="msg">The verbose log message.</param>
        /// <param name="context">The optional <see cref="LoggingContext"/> for additional context.</param>
        void Exception(string division, string msg, Exception ex, LoggingContext context = null);
    }
}
