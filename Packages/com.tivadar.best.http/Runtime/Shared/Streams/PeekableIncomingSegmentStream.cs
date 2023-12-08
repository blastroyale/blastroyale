using Best.HTTP.Shared.PlatformSupport.Network.Tcp;

namespace Best.HTTP.Shared.Streams
{
    public sealed class PeekableIncomingSegmentStream : PeekableStream
    {
        private int peek_listIdx;
        private int peek_pos;

        public override void BeginPeek()
        {
            peek_listIdx = 0;
            peek_pos = base.bufferList.Count > 0 ? base.bufferList[0].Offset : 0;
        }

        public override int PeekByte()
        {
            if (base.bufferList.Count == 0)
                return -1;

            var segment = base.bufferList[this.peek_listIdx];
            if (peek_pos >= segment.Offset + segment.Count)
            {
                if (base.bufferList.Count <= this.peek_listIdx + 1)
                    return -1;

                segment = base.bufferList[++this.peek_listIdx];
                this.peek_pos = segment.Offset;
            }

            return segment.Data[this.peek_pos++];
        }
    }
}
