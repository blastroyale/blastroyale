#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Threading;

using Best.HTTP.Shared.TLS;
using Best.HTTP.Shared.Streams;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls;
using Best.HTTP.Shared.Extensions;

namespace Best.HTTP.Shared.PlatformSupport.Network.Tcp.Streams
{
    public sealed class NonblockingBCTLSStream : PeekableContentProviderStream, ITCPStreamerContentConsumer
    {
        public Action<NonblockingBCTLSStream, TCPStreamer, AbstractTls13Client, Exception> OnNegotiated;

        private TlsClientProtocol _tlsClientProtocol;
        private AbstractTls13Client _tlsClient;

        //private ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private object locker = new object();
        private TCPStreamer _streamer;
        private int _sendBufferSize;
        private bool _disposeStreamer;
        private uint _maxBufferSize;

        private int peek_listIdx;
        private int peek_pos;

        private bool _disposed;

        public NonblockingBCTLSStream(TCPStreamer streamer, TlsClientProtocol tlsClientProtocol, AbstractTls13Client tlsClient, bool disposeStreamer, uint maxBufferSize)
        {
            this._streamer = streamer;
            this._streamer.ContentConsumer = this;
            this._disposeStreamer = disposeStreamer;

            this._sendBufferSize = this._streamer.Socket.SendBufferSize;

            this._tlsClientProtocol = tlsClientProtocol;
            this._tlsClient = tlsClient;

            this.Write(null, 0, 0);

            if (streamer.IsConnectionClosed)
                CallOnNegotiated(new Exception("Connection closed before TLS negotiation started!"));
            _maxBufferSize = maxBufferSize;
        }

        public override void BeginPeek()
        {
            lock (this.locker)
            {
                peek_listIdx = 0;
                peek_pos = base.bufferList.Count > 0 ? base.bufferList[0].Offset : 0;
            }
        }

        public override int PeekByte()
        {
            lock (this.locker)
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

        // Called when content from the server is available
        public void OnContent(TCPStreamer streamer)
        {
            lock (this.locker)
            {
                var socket = this._streamer?.Socket;

                // Ignore content after a TLS client protocol closure (it can happen because of an error, but the server still pumping data to the client).
                if (this._disposed || socket == null || this._tlsClientProtocol.IsClosed)
                {
                    if (this._tlsClientProtocol.IsHandshaking)
                        CallOnNegotiated(new Exception("Connection closed while TLS negotiation is in progress!"));
                    return;
                }

                try
                {
                    PullContentFromStreamer();
                }
                catch (Exception ex)
                {
                    if (!CallOnNegotiated(ex))
                        this.Consumer?.OnError(ex);
                }

                // There's no read/write when it's still hanshaking, so we have to simulate one.
                if (this._tlsClientProtocol.IsHandshaking)
                {
                    this.Write(null, 0, 0);
                    return;
                }
                else
                    CallOnNegotiated(null);

                // Call OnContent only if we have something to offer.
                if (this.Length > 0)
                    this.Consumer?.OnContent();
            }
        }

        public void OnConnectionClosed(TCPStreamer streamer)
        {
            var consumer = this.Consumer;
            if (consumer != null)
                consumer.OnConnectionClosed();
            else
                CallOnNegotiated(new Exception("TCP Connection closed during TLS negotiation!"));
        }

        public void OnError(TCPStreamer streamer, Exception ex)
        {
            var consumer = this.Consumer;
            if (consumer != null)
                consumer.OnError(ex);
            else
                CallOnNegotiated(ex);
        }

        // TODO: It can throw an exception (for example in case of a bad record mac :/), we have to
        //  1.) handle it
        //  2.) report to the consumer (through an OnError call)
        //  3.) prevent other read/write attempts.
        private void PullContentFromStreamer()
        {
            while (!this._disposed && this._streamer.Length > 0 && this._length < this._maxBufferSize)
            {
                var tmp = this._streamer.DequeueReceived();

                if (tmp.Count <= 0)
                {
                    BufferPool.Release(tmp);
                    return;
                }

                try
                {
                    this._tlsClientProtocol.OfferInput(tmp.Data, tmp.Offset, tmp.Count);

                    // each call of OfferInput might generate data (for example alerts) to send to the remote peer!
                    this.Write(null, 0, 0);
                }
                catch (Exception ex)
                {
                    BufferPool.Release(tmp);

                    // each call of OfferInput might generate data (for example alerts) to send to the remote peer!
                    this.Write(null, 0, 0);
                    CallOnNegotiated(ex);

                    throw;
                }

                int available = this._tlsClientProtocol.GetAvailableInputBytes();
                byte[] readBuffer = tmp.Data;
                while (available > 0)
                {
                    if (readBuffer == null)
                        readBuffer = BufferPool.Get(available, true, this._streamer.Context);
                    var readCount = this._tlsClientProtocol.ReadInput(readBuffer, 0, readBuffer.Length);

                    base.Write(readBuffer.AsBuffer(readCount));
                    readBuffer = null;

                    available = this._tlsClientProtocol.GetAvailableInputBytes();
                }

                BufferPool.Release(readBuffer);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (this.locker)
            {
                var readCount = base.Read(buffer, offset, count);

                // pull content from the streamer, if buffered amount is less then the desired.
                if (base.Length <= this._maxBufferSize)
                {
                    try
                    {
                        PullContentFromStreamer();
                    }
                    catch (Exception ex)
                    {
                        this.Consumer.OnError(ex);
                    }
                }

                return readCount;
            }
        }

        // write -> tls encoding -> TCP streamer
        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (this.locker)
            {
                var streamer = this._streamer;
                if (streamer == null)
                    return;

                if (buffer != null && count > 0)
                    this._tlsClientProtocol.WriteApplicationData(buffer, offset, count);

                int available = 0;
                available = this._tlsClientProtocol.GetAvailableOutputBytes();
                while (available > 0)
                {
                    var tmp = BufferPool.Get(this._sendBufferSize, true, streamer.Context);
                    int readCount = 0;

                    try
                    {
                        readCount = this._tlsClientProtocol.ReadOutput(tmp, 0, tmp.Length);
                    }
                    catch
                    {
                        BufferPool.Release(tmp);
                        throw;
                    }

                    streamer.EnqueueToSend(tmp.AsBuffer(readCount));

                    available = this._tlsClientProtocol.GetAvailableOutputBytes();
                }
            }
        }

        public override void Write(BufferSegment bufferSegment)
        {
            lock (this.locker)
            {
                using var _ = new AutoReleaseBuffer(bufferSegment);
                Write(bufferSegment.Data, bufferSegment.Offset, bufferSegment.Count);
            }
        }

        bool CallOnNegotiated(Exception error)
        {
            var callback = Interlocked.CompareExchange(ref this.OnNegotiated, null, this.OnNegotiated);
            if (callback != null)
            {
                try
                {
                    callback(this, this._streamer, this._tlsClient, error);
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(NonblockingBCTLSStream), "CallOnNegotiated", ex, this._streamer.Context);
                }
            }

            return callback != null;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (this._disposed)
                return;

            HTTPManager.Logger.Verbose(nameof(NonblockingBCTLSStream), "Dispose", this._streamer.Context);

            this._disposed = true;
            this._tlsClientProtocol?.Close();

            if (this._disposeStreamer)
                this._streamer?.Dispose();
            this._streamer = null;
            //this._rwLock?.Dispose();
        }
    }
}
#endif
