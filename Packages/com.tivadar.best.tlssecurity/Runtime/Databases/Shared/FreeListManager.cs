using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.TLSSecurity.Databases.Shared
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

            byte[] buffer = BufferPool.Get(8, true);

            try
            {
                if (this.stream.Read(buffer, 0, 4) < 4)
                    return;

                int count = buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];
                for (int i = 0; i < count; ++i)
                {
                    if (this.stream.Read(buffer, 0, 8) < 8)
                    {
                        this.freeList.Clear();
                        return;
                    }

                    int pos = buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];
                    int length = buffer[4] << 24 | buffer[5] << 16 | buffer[6] << 8 | buffer[7];

                    this.freeList.Add(new FreeSpot { pos = pos, length = length });
                }
            }
            finally
            {
                BufferPool.Release(buffer);
            }
        }

        public void Save()
        {
            if (this.freeList.Count == 0)
            {
                this.stream.SetLength(0);
                return;
            }

            byte[] buffer = BufferPool.Get(8, true);
            int count = this.freeList.Count;

            this.stream.SetLength(Math.Min(this.stream.Length, 4 + (count * 8)));
            this.stream.Seek(0, SeekOrigin.Begin);

            buffer[0] = (byte)(count >> 24);
            buffer[1] = (byte)(count >> 16);
            buffer[2] = (byte)(count >> 8);
            buffer[3] = (byte)count;

            this.stream.Write(buffer, 0, 4);

            for (int i = 0; i < count; ++i)
            {
                FreeSpot spot = this.freeList[i];

                int pos = spot.pos;
                buffer[0] = (byte)(pos >> 24);
                buffer[1] = (byte)(pos >> 16);
                buffer[2] = (byte)(pos >> 8);
                buffer[3] = (byte)pos;

                int length = spot.length;
                buffer[4] = (byte)(length >> 24);
                buffer[5] = (byte)(length >> 16);
                buffer[6] = (byte)(length >> 8);
                buffer[7] = (byte)length;

                this.stream.Write(buffer, 0, 8);
            }

            this.stream.Flush();

            BufferPool.Release(buffer);
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
