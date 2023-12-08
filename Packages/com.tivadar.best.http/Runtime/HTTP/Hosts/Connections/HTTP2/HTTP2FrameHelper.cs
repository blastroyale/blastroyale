#if (!UNITY_WEBGL || UNITY_EDITOR) && !BESTHTTP_DISABLE_ALTERNATE_SSL && !BESTHTTP_DISABLE_HTTP2

using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Streams;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;
using System;
using System.Collections.Generic;
using System.IO;

namespace Best.HTTP.Hosts.Connections.HTTP2
{
    // https://httpwg.org/specs/rfc7540.html#ErrorCodes
    public enum HTTP2ErrorCodes
    {
        NO_ERROR = 0x00,
        PROTOCOL_ERROR = 0x01,
        INTERNAL_ERROR = 0x02,
        FLOW_CONTROL_ERROR = 0x03,
        SETTINGS_TIMEOUT = 0x04,
        STREAM_CLOSED = 0x05,
        FRAME_SIZE_ERROR = 0x06,
        REFUSED_STREAM = 0x07,
        CANCEL = 0x08,
        COMPRESSION_ERROR = 0x09,
        CONNECT_ERROR = 0x0A,
        ENHANCE_YOUR_CALM = 0x0B,
        INADEQUATE_SECURITY = 0x0C,
        HTTP_1_1_REQUIRED = 0x0D
    }

    public static class HTTP2FrameHelper
    {
        public static HTTP2ContinuationFrame ReadContinuationFrame(HTTP2FrameHeaderAndPayload header)
        {
            // https://httpwg.org/specs/rfc7540.html#CONTINUATION

            HTTP2ContinuationFrame frame = new HTTP2ContinuationFrame(header);

            frame.HeaderBlockFragment = header.Payload;
            header.Payload = BufferSegment.Empty;

            return frame;
        }

        public static HTTP2WindowUpdateFrame ReadWindowUpdateFrame(HTTP2FrameHeaderAndPayload header)
        {
            // https://httpwg.org/specs/rfc7540.html#WINDOW_UPDATE

            HTTP2WindowUpdateFrame frame = new HTTP2WindowUpdateFrame(header);

            frame.ReservedBit = BufferHelper.ReadBit(header.Payload.Data[0], 0);
            frame.WindowSizeIncrement = BufferHelper.ReadUInt31(header.Payload, 0);

            return frame;
        }

        public static HTTP2GoAwayFrame ReadGoAwayFrame(HTTP2FrameHeaderAndPayload header)
        {
            // https://httpwg.org/specs/rfc7540.html#GOAWAY
            //      str id      error
            // | 0, 1, 2, 3 | 4, 5, 6, 7 | ...

            HTTP2GoAwayFrame frame = new HTTP2GoAwayFrame(header);

            frame.ReservedBit = BufferHelper.ReadBit(header.Payload.Data[0], 0);
            frame.LastStreamId = BufferHelper.ReadUInt31(header.Payload, 0);
            frame.ErrorCode = BufferHelper.ReadUInt32(header.Payload, 4);

            var additionalDebugDataLength = header.Payload.Count - 8;
            if (additionalDebugDataLength > 0)
            {
                var buff = BufferPool.Get(additionalDebugDataLength, true);
                Array.Copy(header.Payload.Data, 8, buff, 0, additionalDebugDataLength);

                frame.AdditionalDebugData = buff.AsBuffer(additionalDebugDataLength);
            }

            return frame;
        }

        public static HTTP2PingFrame ReadPingFrame(HTTP2FrameHeaderAndPayload header)
        {
            // https://httpwg.org/specs/rfc7540.html#PING

            HTTP2PingFrame frame = new HTTP2PingFrame(header);

            Array.Copy(header.Payload.Data, 0, frame.OpaqueData.Data, 0, frame.OpaqueData.Count);

            return frame;
        }

        public static HTTP2PushPromiseFrame ReadPush_PromiseFrame(HTTP2FrameHeaderAndPayload header)
        {
            // https://httpwg.org/specs/rfc7540.html#PUSH_PROMISE

            HTTP2PushPromiseFrame frame = new HTTP2PushPromiseFrame(header);
            int HeaderBlockFragmentLength = header.Payload.Count - 4; // PromisedStreamId

            bool isPadded = (frame.Flags & HTTP2PushPromiseFlags.PADDED) != 0;
            if (isPadded)
            {
                frame.PadLength = header.Payload.Data[0];

                HeaderBlockFragmentLength -= 1 + (frame.PadLength ?? 0);
            }

            frame.ReservedBit = BufferHelper.ReadBit(header.Payload.Data[1], 0);
            frame.PromisedStreamId = BufferHelper.ReadUInt31(header.Payload, 1);

            var HeaderBlockFragmentIdx = isPadded ? 5 : 4;
            frame.HeaderBlockFragment = header.Payload.Slice(HeaderBlockFragmentIdx, HeaderBlockFragmentLength);
            header.Payload = BufferSegment.Empty;

            return frame;
        }

        public static HTTP2RSTStreamFrame ReadRST_StreamFrame(HTTP2FrameHeaderAndPayload header)
        {
            // https://httpwg.org/specs/rfc7540.html#RST_STREAM

            HTTP2RSTStreamFrame frame = new HTTP2RSTStreamFrame(header);
            frame.ErrorCode = BufferHelper.ReadUInt32(header.Payload, 0);

            return frame;
        }

        public static HTTP2PriorityFrame ReadPriorityFrame(HTTP2FrameHeaderAndPayload header)
        {
            // https://httpwg.org/specs/rfc7540.html#PRIORITY

            if (header.Payload.Count != 5)
            {
                //throw FRAME_SIZE_ERROR
            }

            HTTP2PriorityFrame frame = new HTTP2PriorityFrame(header);

            frame.IsExclusive = BufferHelper.ReadBit(header.Payload.Data[0], 0);
            frame.StreamDependency = BufferHelper.ReadUInt31(header.Payload, 0);
            frame.Weight = header.Payload.Data[4];

            return frame;
        }

        public static HTTP2HeadersFrame ReadHeadersFrame(HTTP2FrameHeaderAndPayload header)
        {
            // https://httpwg.org/specs/rfc7540.html#HEADERS

            HTTP2HeadersFrame frame = new HTTP2HeadersFrame(header);
            var HeaderBlockFragmentLength = header.Payload.Count;

            bool isPadded = (frame.Flags & HTTP2HeadersFlags.PADDED) != 0;
            bool isPriority = (frame.Flags & HTTP2HeadersFlags.PRIORITY) != 0;

            int payloadIdx = 0;

            if (isPadded)
            {
                frame.PadLength = header.Payload.Data[payloadIdx++];

                int subLength = 1 + (frame.PadLength ?? 0);
                if (subLength <= HeaderBlockFragmentLength)
                    HeaderBlockFragmentLength -= subLength;
                //else
                //    throw PROTOCOL_ERROR;
            }

            if (isPriority)
            {
                frame.IsExclusive = BufferHelper.ReadBit(header.Payload.Data[payloadIdx], 0);
                frame.StreamDependency = BufferHelper.ReadUInt31(header.Payload, payloadIdx);
                payloadIdx += 4;
                frame.Weight = header.Payload.Data[payloadIdx++];

                int subLength = 5;
                if (subLength <= HeaderBlockFragmentLength)
                    HeaderBlockFragmentLength -= subLength;
                //else
                //    throw PROTOCOL_ERROR;
            }

            var HeaderBlockFragmentIdx = payloadIdx;
            frame.HeaderBlockFragment = header.Payload.Slice(HeaderBlockFragmentIdx, HeaderBlockFragmentLength);

            header.Payload = BufferSegment.Empty;

            return frame;
        }

        public static HTTP2DataFrame ReadDataFrame(HTTP2FrameHeaderAndPayload header)
        {
            // https://httpwg.org/specs/rfc7540.html#DATA

            HTTP2DataFrame frame = new HTTP2DataFrame(header);

            var DataLength = header.Payload.Count;

            bool isPadded = (frame.Flags & HTTP2DataFlags.PADDED) != 0;
            if (isPadded)
            {
                frame.PadLength = header.Payload.Data[0];

                int subLength = 1 + (frame.PadLength ?? 0);
                if (subLength <= DataLength)
                    DataLength -= subLength;
                //else
                //    throw PROTOCOL_ERROR;
            }

            var DataIdx = isPadded ? 1 : 0;

            frame.Data = header.Payload.Slice(DataIdx, DataLength);
            header.Payload = BufferSegment.Empty;

            return frame;
        }

        public static HTTP2AltSVCFrame ReadAltSvcFrame(HTTP2FrameHeaderAndPayload header)
        {
            HTTP2AltSVCFrame frame = new HTTP2AltSVCFrame(header);
            
            // Implement

            return frame;
        }

        public static void StreamRead(Stream stream, byte[] buffer, int offset, uint count)
        {
            if (count == 0)
                return;

            uint sumRead = 0;
            do
            {
                int readCount = (int)(count - sumRead);
                int streamReadCount = stream.Read(buffer, (int)(offset + sumRead), readCount);
                if (streamReadCount <= 0 && readCount > 0)
                    throw new Exception("TCP Stream closed!");
                sumRead += (uint)streamReadCount;
            } while (sumRead < count);
        }

        public static void StreamRead(Stream stream, BufferSegment buffer)
        {
            if (buffer.Count == 0)
                return;

            uint sumRead = 0;
            do
            {
                int readCount = (int)(buffer.Count - sumRead);
                int streamReadCount = stream.Read(buffer.Data, (int)(buffer.Offset + sumRead), readCount);
                if (streamReadCount <= 0 && readCount > 0)
                    throw new Exception("TCP Stream closed!");
                sumRead += (uint)streamReadCount;
            } while (sumRead < buffer.Count);
        }

        public static AutoReleaseBuffer HeaderAsBinary(HTTP2FrameHeaderAndPayload header)
        {
            // https://httpwg.org/specs/rfc7540.html#FrameHeader

            var buffer = BufferPool.Get(9, true);

            BufferHelper.SetUInt24(buffer, 0, (uint)header.Payload.Count);
            buffer[3] = (byte)header.Type;
            buffer[4] = header.Flags;
            BufferHelper.SetUInt31(buffer, 5, header.StreamId);

            return new AutoReleaseBuffer { Data = buffer, Count = 9 };
        }

        public unsafe static bool CanReadFullFrame(PeekableStream stream)
        {
            // https://httpwg.org/specs/rfc7540.html#FrameHeader

            // A frame without any payload is 9 bytes
            if (stream.Length < 9)
                return false;

            stream.BeginPeek();

            // First 3 bytes are the payload length
            var rawLength = stackalloc byte[3];
            rawLength[0] = (byte)stream.PeekByte();
            rawLength[1] = (byte)stream.PeekByte();
            rawLength[2] = (byte)stream.PeekByte();

            var payloadLength = (UInt32)(rawLength[2] | rawLength[1] << 8 | rawLength[0] << 16);

            return stream.Length >= (9 + payloadLength);
        }

        public static HTTP2FrameHeaderAndPayload ReadHeader(Stream stream, LoggingContext context)
        {
            byte[] buffer = BufferPool.Get(9, true, context);
            using var _ = buffer.AsAutoRelease();

            StreamRead(stream, buffer, 0, 9);

            HTTP2FrameHeaderAndPayload header = new HTTP2FrameHeaderAndPayload();

            var PayloadLength = (int)BufferHelper.ReadUInt24(buffer, 0);
            header.Type = (HTTP2FrameTypes)buffer[3];
            header.Flags = buffer[4];
            header.StreamId = BufferHelper.ReadUInt31(buffer, 5);

            header.Payload = BufferPool.Get(PayloadLength, true, context).AsBuffer(PayloadLength);

            try
            {
                StreamRead(stream, header.Payload);
            }
            catch
            {
                BufferPool.Release(header.Payload);
                throw;
            }

            return header;
        }

        public static HTTP2SettingsFrame ReadSettings(HTTP2FrameHeaderAndPayload header)
        {
            HTTP2SettingsFrame frame = new HTTP2SettingsFrame(header);

            if (header.Payload.Count > 0)
            {
                int kvpCount = (int)(header.Payload.Count / 6);

                frame.Settings = new List<KeyValuePair<HTTP2Settings, uint>>(kvpCount);
                for (int i = 0; i < kvpCount; ++i)
                {
                    HTTP2Settings key = (HTTP2Settings)BufferHelper.ReadUInt16(header.Payload.Data, i * 6);
                    UInt32 value = BufferHelper.ReadUInt32(header.Payload, (i * 6) + 2);

                    frame.Settings.Add(new KeyValuePair<HTTP2Settings, uint>(key, value));
                }
            }

            return frame;
        }

        public static HTTP2FrameHeaderAndPayload CreateACKSettingsFrame()
        {
            HTTP2FrameHeaderAndPayload frame = new HTTP2FrameHeaderAndPayload();
            frame.Type = HTTP2FrameTypes.SETTINGS;
            frame.Flags = (byte)HTTP2SettingsFlags.ACK;

            return frame;
        }

        public static HTTP2FrameHeaderAndPayload CreateSettingsFrame(List<KeyValuePair<HTTP2Settings, UInt32>> settings, LoggingContext context)
        {
            HTTP2FrameHeaderAndPayload frame = new HTTP2FrameHeaderAndPayload();
            frame.Type = HTTP2FrameTypes.SETTINGS;
            frame.Flags = 0;
            var PayloadLength = settings.Count * 6;

            frame.Payload = BufferPool.Get(PayloadLength, true, context).AsBuffer(PayloadLength);

            for (int i = 0; i < settings.Count; ++i)
            {
                BufferHelper.SetUInt16(frame.Payload.Data, i * 6, (UInt16)settings[i].Key);
                BufferHelper.SetUInt32(frame.Payload.Data, (i * 6) + 2, settings[i].Value);
            }

            return frame;
        }

        public static HTTP2FrameHeaderAndPayload CreatePingFrame(HTTP2PingFlags flags, LoggingContext context)
        {
            // https://httpwg.org/specs/rfc7540.html#PING

            HTTP2FrameHeaderAndPayload frame = new HTTP2FrameHeaderAndPayload();
            frame.Type = HTTP2FrameTypes.PING;
            frame.Flags = (byte)flags;
            frame.StreamId = 0;
            frame.Payload = BufferPool.Get(8, true, context).AsBuffer(8);

            return frame;
        }

        public static HTTP2FrameHeaderAndPayload CreateWindowUpdateFrame(UInt32 streamId, UInt32 windowSizeIncrement, LoggingContext context)
        {
            // https://httpwg.org/specs/rfc7540.html#WINDOW_UPDATE

            HTTP2FrameHeaderAndPayload frame = new HTTP2FrameHeaderAndPayload();
            frame.Type = HTTP2FrameTypes.WINDOW_UPDATE;
            frame.Flags = 0;
            frame.StreamId = streamId;
            frame.Payload = BufferPool.Get(4, true, context).AsBuffer(4);

            BufferHelper.SetBit(0, 0, 0);
            BufferHelper.SetUInt31(frame.Payload.Data, 0, windowSizeIncrement);

            return frame;
        }

        public static HTTP2FrameHeaderAndPayload CreateGoAwayFrame(UInt32 lastStreamId, HTTP2ErrorCodes error, LoggingContext context)
        {
            // https://httpwg.org/specs/rfc7540.html#GOAWAY

            HTTP2FrameHeaderAndPayload frame = new HTTP2FrameHeaderAndPayload();
            frame.Type = HTTP2FrameTypes.GOAWAY;
            frame.Flags = 0;
            frame.StreamId = 0;
            frame.Payload = BufferPool.Get(8, true, context).AsBuffer(8);

            BufferHelper.SetUInt31(frame.Payload.Data, 0, lastStreamId);
            BufferHelper.SetUInt31(frame.Payload.Data, 4, (UInt32)error);

            return frame;
        }

        public static HTTP2FrameHeaderAndPayload CreateRSTFrame(UInt32 streamId, HTTP2ErrorCodes errorCode, LoggingContext context)
        {
            // https://httpwg.org/specs/rfc7540.html#RST_STREAM

            HTTP2FrameHeaderAndPayload frame = new HTTP2FrameHeaderAndPayload();
            frame.Type = HTTP2FrameTypes.RST_STREAM;
            frame.Flags = 0;
            frame.StreamId = streamId;
            frame.Payload = BufferPool.Get(4, true, context).AsBuffer(4);

            BufferHelper.SetUInt32(frame.Payload.Data, 0, (UInt32)errorCode);

            return frame;
        }
    }
}

#endif
