using System.Collections.Generic;
using System.Linq;

namespace Best.HTTP.Shared.Logger
{
    /// <summary>
    /// Represents a logging context for categorizing and organizing log messages.
    /// </summary>
    /// <remarks>
    /// The LoggingContext class is used to provide additional context information
    /// to log messages, allowing for better categorization and organization of log output. It can be
    /// associated with specific objects or situations to enrich log entries with context-specific data.
    /// </remarks>
    public sealed class LoggingContext
    {
        /// <summary>
        /// Gets the unique hash value of this logging context.
        /// </summary>
        public string Hash { get; private set; }

        private enum LoggingContextFieldType
        {
            Long,
            Bool,
            String,
            AnotherContext
        }

        private struct LoggingContextField
        {
            public string key;
            public long longValue;
            public bool boolValue;
            public string stringValue;
            public LoggingContext loggingContextValue;
            public LoggingContextFieldType fieldType;

            public override string ToString()
            {
                object value = this.fieldType switch
                {
                    LoggingContextFieldType.Bool => this.boolValue,
                    LoggingContextFieldType.Long => this.longValue,
                    LoggingContextFieldType.String => this.stringValue,
                    _ => this.loggingContextValue
                };

                return $"[{this.key} => '{value}']";
            }
        }

        private List<LoggingContextField> fields = new List<LoggingContextField>(2);

        /// <summary>
        /// Initializes a new instance of the LoggingContext class associated with the specified object.
        /// </summary>
        /// <param name="boundto">The object to associate the context with.</param>
        public LoggingContext(object boundto)
        {
            var name = boundto.GetType().Name;
            Add("TypeName", name);

            UnityEngine.Hash128 hash = new UnityEngine.Hash128();
            hash.Append(name);
            hash.Append(boundto.GetHashCode());
            hash.Append(this.GetHashCode());

            this.Hash = hash.ToString();
            Add("Hash", this.Hash);
        }

        /// <summary>
        /// Adds a <c>long</c> value to the logging context.
        /// </summary>
        /// <param name="key">The key to associate with the value.</param>
        /// <param name="value">The <c>long</c> value to add.</param>
        public void Add(string key, long value) => Add(new LoggingContextField { fieldType = LoggingContextFieldType.Long, key = key, longValue = value });

        /// <summary>
        /// Adds a <c>bool</c> value to the logging context.
        /// </summary>
        /// <param name="key">The key to associate with the value.</param>
        /// <param name="value">The <c>bool</c> value to add.</param>
        public void Add(string key, bool value) => Add(new LoggingContextField { fieldType = LoggingContextFieldType.Bool, key = key, boolValue = value });

        /// <summary>
        /// Adds a <c>string</c> value to the logging context.
        /// </summary>
        /// <param name="key">The key to associate with the value.</param>
        /// <param name="value">The <c>string</c> value to add.</param>
        public void Add(string key, string value) => Add(new LoggingContextField { fieldType = LoggingContextFieldType.String, key = key, stringValue = value });

        /// <summary>
        /// Adds a <c>LoggingContext</c> value to the logging context.
        /// </summary>
        /// <param name="key">The key to associate with the value.</param>
        /// <param name="value">The <c>LoggingContext</c> value to add.</param>
        public void Add(string key, LoggingContext value) => Add(new LoggingContextField { fieldType = LoggingContextFieldType.AnotherContext, key = key, loggingContextValue = value });

        private void Add(LoggingContextField field)
        {
            Remove(field.key);
            this.fields.Add(field);
        }

        /// <summary>
        /// Gets the <c>string</c> field with the specified name from the logging context.
        /// </summary>
        /// <param name="fieldName">The name of the <c>string</c> field to retrieve.</param>
        /// <returns>The value of the <c>string</c> field or <c>null</c> if not found.</returns>
        public string GetStringField(string fieldName) => this.fields.FirstOrDefault(f => f.key == fieldName).stringValue;

        /// <summary>
        /// Removes a field from the logging context by its key.
        /// </summary>
        /// <param name="key">The key of the field to remove.</param>
        public void Remove(string key) => this.fields.RemoveAll(field => field.key == key);

        /// <summary>
        /// Converts the logging context and its associated fields to a JSON string representation.
        /// </summary>
        /// <param name="sb">A <see cref="System.Text.StringBuilder"/> instance to which the JSON string is appended.</param>
        /// <remarks>
        /// This method serializes the logging context and its associated fields into a JSON format
        /// for structured logging purposes. The resulting JSON string represents the context and its fields, making it
        /// suitable for inclusion in log entries for better analysis and debugging.
        /// </remarks>
        public void ToJson(System.Text.StringBuilder sb)
        {
            if (this.fields == null || this.fields.Count == 0)
            {
                sb.Append("null");
                return;
            }

            sb.Append("{");
            for (int i = 0; i < this.fields.Count; ++i)
            {
                var field = this.fields[i];

                if (field.fieldType != LoggingContextFieldType.AnotherContext)
                {
                    if (i > 0)
                        sb.Append(", ");

                    sb.AppendFormat("\"{0}\": ", field.key);
                }

                switch (field.fieldType)
                {
                    case LoggingContextFieldType.Long:
                        sb.Append(field.longValue);
                        break;
                    case LoggingContextFieldType.Bool:
                        sb.Append(field.boolValue ? "true" : "false");
                        break;
                    case LoggingContextFieldType.String:
                        sb.AppendFormat("\"{0}\"", Escape(field.stringValue));
                        break;
                }
            }

            sb.Append("}");

            for (int i = 0; i < this.fields.Count; ++i)
            {
                var field = this.fields[i];

                switch (field.fieldType)
                {
                    case LoggingContextFieldType.AnotherContext:
                        sb.Append(", ");
                        field.loggingContextValue.ToJson(sb);
                        break;
                }
            }
        }

        public static string Escape(string original)
        {
            return Best.HTTP.Shared.PlatformSupport.Text.StringBuilderPool.ReleaseAndGrab(Best.HTTP.Shared.PlatformSupport.Text.StringBuilderPool.Get(1)
                        .Append(original)
                        .Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("/", "\\/")
                        .Replace("\b", "\\b")
                        .Replace("\f", "\\f")
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t"));
        }
    }
}
