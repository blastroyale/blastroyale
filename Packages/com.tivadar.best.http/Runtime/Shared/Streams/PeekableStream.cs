namespace Best.HTTP.Shared.Streams
{
    public abstract class PeekableStream : BufferSegmentStream
    {
        public abstract void BeginPeek();
        public abstract int PeekByte();
    }

}
