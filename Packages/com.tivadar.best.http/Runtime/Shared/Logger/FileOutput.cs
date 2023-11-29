using System;
using System.IO;

using Best.HTTP.Shared.PlatformSupport.FileSystem;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Shared.Logger
{
    /// <summary>
    /// Provides an implementation of <see cref="ILogOutput"/> that writes log messages to a file.
    /// </summary>
    public sealed class FileOutput : ILogOutput
    {
        /// <summary>
        /// Gets a value indicating whether this log output accepts color codes. Always returns <c>false</c>.
        /// </summary>
        public bool AcceptColor { get; } = false;

        private Stream fileStream;

        /// <summary>
        /// Initializes a new instance of the FileOutput class with the specified file name.
        /// </summary>
        /// <param name="fileName">The name of the file to write log messages to.</param>
        public FileOutput(string fileName)
        {
            // Create a buffered stream for writing log messages to the specified file.
            this.fileStream = new BufferedStream(HTTPManager.IOService.CreateFileStream(fileName, FileStreamModes.Create), 512 * 1024);
        }

        /// <summary>
        /// Writes a log message to the file.
        /// </summary>
        /// <param name="level">The log level of the message.</param>
        /// <param name="logEntry">The log message to write.</param>
        public void Write(Loglevels level, string logEntry)
        {
            if (this.fileStream != null && !string.IsNullOrEmpty(logEntry))
            {
                int count = System.Text.Encoding.UTF8.GetByteCount(logEntry) + 2;
                var buffer = BufferPool.Get(count, true);

                try
                {
                    System.Text.Encoding.UTF8.GetBytes(logEntry, 0, logEntry.Length, buffer, 0);
                    buffer[count - 2] = (byte)'\r';
                    buffer[count - 1] = (byte)'\n';

                    this.fileStream.Write(buffer, 0, count);
                }
                finally
                {
                    BufferPool.Release(buffer);
                }
            }
        }

        /// <summary>
        /// Flushes any buffered log messages to the file.
        /// </summary>
        public void Flush() => this.fileStream?.Flush();

        /// <summary>
        /// Releases any resources used by the FileOutput instance.
        /// </summary>
        public void Dispose()
        {
            if (this.fileStream != null)
            {
                this.fileStream.Close();
                this.fileStream = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}
