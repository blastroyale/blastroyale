using System;
using System.Collections.Generic;
using System.IO;

using Best.HTTP.Shared.Databases.Utils;

namespace Best.HTTP.Shared.Databases
{
    public sealed class FreeListManager : IDisposable
    {
        struct FreeSpot
        {
            public int pos;
            public int length;
        }

        private Stream stream;
        private List<FreeSpot> freeList = new List<FreeSpot>();

        public FreeListManager(Stream stream)
        {
            this.stream = stream;
            Load();
        }

        private void Load()
        {
            this.freeList.Clear();
            this.stream.Seek(0, SeekOrigin.Begin);

            if (this.stream.Length == 0)
                return;

            try
            {
                uint count = (uint)stream.DecodeUnsignedVariableByteInteger();
                for (int i = 0; i < count; ++i)
                {
                    int pos = (int)stream.DecodeUnsignedVariableByteInteger();
                    int length = (int)stream.DecodeUnsignedVariableByteInteger();

                    this.freeList.Add(new FreeSpot { pos = pos, length = length });
                }
            }
            catch
            {
                this.freeList.Clear();
                this.stream.SetLength(0);
            }
        }

        public void Save()
        {
            if (this.freeList.Count == 0)
            {
                this.stream.SetLength(0);
                return;
            }

            int count = this.freeList.Count;

            this.stream.Seek(0, SeekOrigin.Begin);

            stream.EncodeUnsignedVariableByteInteger((uint)count);

            for (int i = 0; i < count; ++i)
            {
                FreeSpot spot = this.freeList[i];

                stream.EncodeUnsignedVariableByteInteger((uint)spot.pos);
                stream.EncodeUnsignedVariableByteInteger((uint)spot.length);
            }

            this.stream.Flush();
        }

        public int FindFreeIndex(int length)
        {
            for (int i = 0; i < this.freeList.Count; ++i)
            {
                FreeSpot spot = this.freeList[i];

                if (spot.length >= length)
                    return i;
            }

            return -1;
        }

        public int Occupy(int idx, int length)
        {
            FreeSpot spot = this.freeList[idx];
            int position = spot.pos;

            if (spot.length < length)
                throw new Exception($"Can't Occupy a free spot with smaller space ({spot.length} < {length})!");

            if (spot.length > length)
            {
                spot.pos += length;
                spot.length -= length;

                this.freeList[idx] = spot;
            }
            else
                this.freeList.RemoveAt(idx);

            return position;
        }

        public void Add(int pos, int length)
        {
            int insertToIdx = 0;

            while (insertToIdx < this.freeList.Count && this.freeList[insertToIdx].pos < pos)
                insertToIdx++;

            if (insertToIdx > this.freeList.Count)
                throw new Exception($"Couldn't find free spot with position '{pos}'!");

            bool merged = false;
            FreeSpot spot = new FreeSpot { pos = pos, length = length };

            if (insertToIdx > 0)
            {
                var prev = this.freeList[insertToIdx - 1];

                // Merge with previous
                if (prev.pos + prev.length == pos)
                {
                    prev.length += length;

                    this.freeList[insertToIdx - 1] = prev;

                    spot = prev;
                    merged = true;
                }
            }

            if (insertToIdx < this.freeList.Count)
            {
                var next = this.freeList[insertToIdx];

                // merge with next?
                if (spot.pos + spot.length == next.pos)
                {
                    spot.length += next.length;

                    if (!merged)
                    {
                        // Not already merged, extend the one in place
                        this.freeList[insertToIdx] = spot;
                        merged = true;
                    }
                    else
                    {
                        // Already merged. Further extend the previous, and remove the next.
                        this.freeList[insertToIdx - 1] = spot;
                        this.freeList.RemoveAt(insertToIdx);
                    }
                }
            }

            if (!merged)
                this.freeList.Insert(insertToIdx, spot);
        }

        public void Clear()
        {
            this.freeList.Clear();
        }

        public void Dispose()
        {
            if (this.stream != null)
                this.stream.Close();
            this.stream = null;
            GC.SuppressFinalize(this);
        }
    }
}
