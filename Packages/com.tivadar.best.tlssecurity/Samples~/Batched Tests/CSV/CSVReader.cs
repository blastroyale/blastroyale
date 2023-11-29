using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Best.TLSSecurity.Examples.CSV
{
    public sealed class CSVValue
    {
        public string Value;
    }

    public sealed class CSVRow
    {
        public List<CSVValue> Values = new List<CSVValue>();
    }

    public sealed class CSVColumn
    {
        public string Name;
        public List<CSVValue> Values = new List<CSVValue>();
    }

    public sealed class CSVDB
    {
        public List<CSVColumn> Columns = new List<CSVColumn>();
        public List<CSVRow> Rows = new List<CSVRow>();
    }

    public static class CSVReader
    {
        public static CSVDB Read(Stream file)
        {
            CSVDB db = new CSVDB();

            using (var reader = new StreamReader(file, System.Text.Encoding.UTF8))
            {
                var definition = ReadDefinition(reader);

                foreach (var name in definition.ColumnNames)
                    db.Columns.Add(new CSVColumn { Name = name });

                int columnIdx = 0;
                CSVRow currentRow = null;
                while (!reader.EndOfStream)
                {
                    var value = ReadValue(reader, definition.Separator, out _);

                    db.Columns[columnIdx].Values.Add(value);

                    if (currentRow == null)
                        db.Rows.Add(currentRow = new CSVRow());

                    currentRow.Values.Add(value);

                    columnIdx = ++columnIdx % definition.ColumnCount;
                    if (columnIdx == 0)
                        currentRow = null;
                }
            }

            return db;
        }

        // Year, Make,"Mo""del","Desc
        //    ription","Pri, ce"
        // 1997,Ford,E350,"ac, abs, moon",3000.00
        // 1999,Chevy,"Venture ""Extended Edition""","",4900.00
        // 1999,Chevy,"Venture ""Extended Edition, Very Large""",,5000.00
        // 1996,Jeep,Grand Cherokee,"MUST SELL!
        // air, moon roof, loaded",4799.00

        private static StringBuilder builder = new StringBuilder();
        private static CSVValue ReadValue(StreamReader reader, char separator, out bool end)
        {
            builder.Clear();

            char current = (char)reader.Read();

            if (current == ',')
            {
                end = false;
                return new CSVValue { Value = "" };
            }

            bool isBlockQuoted = current == '"' /*&& (char)reader.Peek() != '"'*/;
            int blockQouteCount = isBlockQuoted ? 1 : 0;

            //bool isOnlyWhiteSpace = char.IsWhiteSpace(current);

            if (isBlockQuoted)
                current = (char)reader.Read();

            bool lineEnding;

            do
            {
                if (current == '"')
                {
                    if ((char)reader.Peek() == '"')
                    {
                        reader.Read();
                        builder.Append('"');
                    }
                    else
                    {
                        blockQouteCount--;

                        if (blockQouteCount == 0)
                        {
                            // read until end
                            char next = (char)reader.Peek();
                            while (next != '\n' && next != separator && !reader.EndOfStream)
                            {
                                current = (char)reader.Read();
                                next = (char)reader.Peek();
                            }
                        }
                    }
                }
                else
                {
                    builder.Append(current);
                }

                current = (char)reader.Read();
                lineEnding = IsLineEnd(reader, current);
            } while ((isBlockQuoted && blockQouteCount > 0) || (!isBlockQuoted && current != separator && !lineEnding));

            end = lineEnding;

            if (lineEnding && current == '\r' && (char)reader.Peek() == '\n')
                reader.Read();

            return new CSVValue { Value = builder.ToString() };
        }

        private static bool IsLineEnd(StreamReader reader, char current)
        {
            char next = (char)reader.Peek();
            return current == '\n' || current == '\r' && next != '\n' || current == '\r' && next == '\n' || current == (char)'\uFFFF';
        }

        private static CSVDefinition ReadDefinition(StreamReader reader)
        {
            CSVDefinition def = new CSVDefinition() { Separator = ',' };

            bool endReached = false;
            do
            {
                var value = ReadValue(reader, def.Separator, out endReached);
                def.ColumnNames.Add(value.Value);
                def.ColumnCount++;
            } while (!endReached);

            return def;
        }

        private sealed class CSVDefinition
        {
            public char Separator;
            public List<string> ColumnNames = new List<string>();
            public int ColumnCount;
        }
    }
}
