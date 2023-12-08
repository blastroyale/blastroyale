#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Text;
using System.Threading;

using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.PlatformSupport.Network.Tcp;
using Best.HTTP.Shared.Streams;

namespace Best.HTTP.Proxies.Implementations
{
    internal enum SOCKSVersions : byte
    {
        Unknown = 0x00,
        V5 = 0x05
    }

    /// <summary>
    /// https://tools.ietf.org/html/rfc1928
    ///   The values currently defined for METHOD are:
    ///     o  X'00' NO AUTHENTICATION REQUIRED
    ///     o  X'01' GSSAPI
    ///     o  X'02' USERNAME/PASSWORD
    ///     o  X'03' to X'7F' IANA ASSIGNED
    ///     o  X'80' to X'FE' RESERVED FOR PRIVATE METHODS
    ///     o  X'FF' NO ACCEPTABLE METHODS
    /// </summary>
    internal enum SOCKSMethods : byte
    {
        NoAuthenticationRequired = 0x00,
        GSSAPI = 0x01,
        UsernameAndPassword = 0x02,
        NoAcceptableMethods = 0xFF
    }

    internal enum SOCKSReplies : byte
    {
        Succeeded = 0x00,
        GeneralSOCKSServerFailure = 0x01,
        ConnectionNotAllowedByRuleset = 0x02,
        NetworkUnreachable = 0x03,
        HostUnreachable = 0x04,
        ConnectionRefused = 0x05,
        TTLExpired = 0x06,
        CommandNotSupported = 0x07,
        AddressTypeNotSupported = 0x08
    }

    internal enum SOCKSAddressTypes
    {
        IPV4 = 0x00,
        DomainName = 0x03,
        IPv6 = 0x04
    }

    internal sealed class SOCKSV5Negotiator : IContentConsumer
    {
        public PeekableContentProviderStream ContentProvider { get; private set; }

        enum NegotiationStates
        {
            MethodSelection,
            ExpectAuthenticationResponse,
            ConnectResponse
        }

        NegotiationStates _state;

        SOCKSProxy _proxy;
        ProxyConnectParameters _parameters;

        public SOCKSV5Negotiator(SOCKSProxy proxy, ProxyConnectParameters parameters)
        {
            this._proxy = proxy;
            this._parameters = parameters;

            //(this._parameters.stream as IPeekableContentProvider).Consumer = this;
            (this._parameters.stream as PeekableContentProviderStream).SetTwoWayBinding(this);

            SendHandshake();
        }

        public void SetBinding(PeekableContentProviderStream contentProvider) => this.ContentProvider = contentProvider;
        public void UnsetBinding() => this.ContentProvider = null;

        public void OnConnectionClosed()
        {
            CallOnError(new Exception($"{nameof(SOCKSV5Negotiator)}: connection closed unexpectedly!"));
        }

        public void OnError(Exception ex)
        {
            CallOnError(ex);
        }

        void SendHandshake()
        {
            var buffer = BufferPool.Get(1024, true);
            try
            {
                int count = 0;

                // https://tools.ietf.org/html/rfc1928
                //   The client connects to the server, and sends a version
                //   identifier/method selection message:
                //
                //                   +----+----------+----------+
                //                   |VER | NMETHODS | METHODS  |
                //                   +----+----------+----------+
                //                   | 1  |    1     | 1 to 255 |
                //                   +----+----------+----------+
                //
                //   The VER field is set to X'05' for this version of the protocol.  The
                //   NMETHODS field contains the number of method identifier octets that
                //   appear in the METHODS field.
                //

                buffer[count++] = (byte)SOCKSVersions.V5;
                if (this._proxy.Credentials != null)
                {
                    buffer[count++] = 0x02; // method count
                    buffer[count++] = (byte)SOCKSMethods.UsernameAndPassword;
                    buffer[count++] = (byte)SOCKSMethods.NoAuthenticationRequired;
                }
                else
                {
                    buffer[count++] = 0x01; // method count
                    buffer[count++] = (byte)SOCKSMethods.NoAuthenticationRequired;
                }

                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Information("SOCKSProxy", $"Sending method negotiation - buffer: {buffer.AsBuffer(count)} ", this._parameters.context);

                // enqueue buffer and move its ownership to the tcp streamer
                this._parameters.stream.Write(buffer.AsBuffer(count));

                // null out the buffer so it won't be released 
                buffer = null;

            }
            catch (Exception ex)
            {
                CallOnError(ex);
            }
            finally
            {
                BufferPool.Release(buffer);
            }
        }

        void SendConnect()
        {
            //   The SOCKS request is formed as follows:
            //
            //        +----+-----+-------+------+----------+----------+
            //        |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
            //        +----+-----+-------+------+----------+----------+
            //        | 1  |  1  | X'00' |  1   | Variable |    2     |
            //        +----+-----+-------+------+----------+----------+
            //
            //     Where:
            //
            //          o  VER    protocol version: X'05'
            //          o  CMD
            //             o  CONNECT X'01'
            //             o  BIND X'02'
            //             o  UDP ASSOCIATE X'03'
            //          o  RSV    RESERVED
            //          o  ATYP   address type of following address
            //             o  IP V4 address: X'01'
            //             o  DOMAINNAME: X'03'
            //             o  IP V6 address: X'04'
            //          o  DST.ADDR       desired destination address
            //          o  DST.PORT desired destination port in network octet
            //             order

            var buffer = BufferPool.Get(512, true);
            int count = 0;
            buffer[count++] = (byte)SOCKSVersions.V5; // version: 5
            buffer[count++] = 0x01; // command: connect
            buffer[count++] = 0x00; // reserved, bust be 0x00

            if (this._parameters.uri.IsHostIsAnIPAddress())
            {
                bool isIPV4 = Extensions.IsIpV4AddressValid(this._parameters.uri.Host);
                buffer[count++] = isIPV4 ? (byte)SOCKSAddressTypes.IPV4 : (byte)SOCKSAddressTypes.IPv6;

                var ipAddress = System.Net.IPAddress.Parse(this._parameters.uri.Host);
                var ipBytes = ipAddress.GetAddressBytes();
                WriteBytes(buffer, ref count, ipBytes); // destination address
            }
            else
            {
                buffer[count++] = (byte)SOCKSAddressTypes.DomainName;

                // The first octet of the address field contains the number of octets of name that
                // follow, there is no terminating NUL octet.
                WriteString(buffer, ref count, this._parameters.uri.Host);
            }

            // destination port in network octet order
            buffer[count++] = (byte)((this._parameters.uri.Port >> 8) & 0xFF);
            buffer[count++] = (byte)(this._parameters.uri.Port & 0xFF);

            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Information("SOCKSProxy", $"Sending connect request - buffer: {buffer.AsBuffer(count)} ", this._parameters.context);

            this._parameters.stream.Write(buffer.AsBuffer(count));

            this._state = NegotiationStates.ConnectResponse;
        }

        public void OnContent()
        {
            try
            {
                switch (this._state)
                {
                    case NegotiationStates.MethodSelection:
                        {
                            if (this.ContentProvider.Length < 2)
                                return;

                            // Read method selection result

                            //count = stream.Read(buffer, 0, buffer.Length);
                            var buffer = BufferPool.Get(BufferPool.MinBufferSize, true);
                            int count = this.ContentProvider.Read(buffer, 0, buffer.Length);

                            if (HTTPManager.Logger.IsDiagnostic)
                                HTTPManager.Logger.Information("SOCKSProxy", $"Negotiation response - count: {count} buffer: {buffer.AsBuffer(count)}", this._parameters.context);

                            //   The server selects from one of the methods given in METHODS, and
                            //   sends a METHOD selection message:
                            //
                            //                         +----+--------+
                            //                         |VER | METHOD |
                            //                         +----+--------+
                            //                         | 1  |   1    |
                            //                         +----+--------+
                            //
                            //   If the selected METHOD is X'FF', none of the methods listed by the
                            //   client are acceptable, and the client MUST close the connection.
                            //
                            //   The values currently defined for METHOD are:
                            //
                            //          o  X'00' NO AUTHENTICATION REQUIRED
                            //          o  X'01' GSSAPI
                            //          o  X'02' USERNAME/PASSWORD
                            //          o  X'03' to X'7F' IANA ASSIGNED
                            //          o  X'80' to X'FE' RESERVED FOR PRIVATE METHODS
                            //          o  X'FF' NO ACCEPTABLE METHODS
                            //
                            //   The client and server then enter a method-specific sub-negotiation.

                            SOCKSVersions version = (SOCKSVersions)buffer[0];
                            SOCKSMethods method = (SOCKSMethods)buffer[1];

                            // Expected result:
                            //  1.) Received bytes' count is 2: version + preferred method
                            //  2.) Version must be 5
                            //  3.) Preferred method must NOT be 0xFF
                            if (count != 2)
                                throw new Exception($"SOCKS Proxy - Expected read count: 2! buffer: {buffer.AsBuffer(count)}");
                            else if (version != SOCKSVersions.V5)
                                throw new Exception("SOCKS Proxy - Expected version: 5, received version: " + buffer[0].ToString("X2"));
                            else if (method == SOCKSMethods.NoAcceptableMethods)
                                throw new Exception("SOCKS Proxy - Received 'NO ACCEPTABLE METHODS' (0xFF)");
                            else
                            {
                                HTTPManager.Logger.Information("SOCKSProxy", "Method negotiation over. Method: " + method.ToString(), this._parameters.context);
                                switch (method)
                                {
                                    case SOCKSMethods.NoAuthenticationRequired:
                                        SendConnect();
                                        break;

                                    case SOCKSMethods.UsernameAndPassword:
                                        if (this._proxy.Credentials.UserName.Length > 255)
                                            throw new Exception($"SOCKS Proxy - Credentials.UserName too long! {this._proxy.Credentials.UserName.Length} > 255");
                                        if (this._proxy.Credentials.Password.Length > 255)
                                            throw new Exception($"SOCKS Proxy - Credentials.Password too long! {this._proxy.Credentials.Password.Length} > 255");

                                        // https://tools.ietf.org/html/rfc1929 : Username/Password Authentication for SOCKS V5
                                        //   Once the SOCKS V5 server has started, and the client has selected the
                                        //   Username/Password Authentication protocol, the Username/Password
                                        //   subnegotiation begins.  This begins with the client producing a
                                        //   Username/Password request:
                                        //
                                        //           +----+------+----------+------+----------+
                                        //           |VER | ULEN |  UNAME   | PLEN |  PASSWD  |
                                        //           +----+------+----------+------+----------+
                                        //           | 1  |  1   | 1 to 255 |  1   | 1 to 255 |
                                        //           +----+------+----------+------+----------+

                                        HTTPManager.Logger.Information("SOCKSProxy", "starting sub-negotiation", this._parameters.context);
                                        count = 0;
                                        buffer[count++] = 0x01; // version of sub negotiation

                                        WriteString(buffer, ref count, this._proxy.Credentials.UserName);
                                        WriteString(buffer, ref count, this._proxy.Credentials.Password);

                                        if (HTTPManager.Logger.IsDiagnostic)
                                            HTTPManager.Logger.Information("SOCKSProxy", $"Sending username and password sub-negotiation - buffer: {buffer.AsBuffer(count)} ", this._parameters.context);

                                        // Write negotiation and transfer ownership of buffer
                                        this._parameters.stream.Write(buffer.AsBuffer(count));

                                        this._state = NegotiationStates.ExpectAuthenticationResponse;
                                        break;

                                    case SOCKSMethods.GSSAPI:
                                        throw new Exception("SOCKS proxy: GSSAPI not supported!");

                                    case SOCKSMethods.NoAcceptableMethods:
                                        throw new Exception("SOCKS proxy: No acceptable method");
                                }
                            }
                            break;
                        }

                    case NegotiationStates.ExpectAuthenticationResponse:
                        {
                            if (this.ContentProvider.Length < 2)
                                return;

                            // Read result
                            var buffer = BufferPool.Get(512, true);
                            var count = this._parameters.stream.Read(buffer, 0, buffer.Length);

                            if (HTTPManager.Logger.IsDiagnostic)
                                HTTPManager.Logger.Information("SOCKSProxy", $"Username and password sub-negotiation response - buffer: {buffer.AsBuffer(count)} ", this._parameters.context);

                            //   The server verifies the supplied UNAME and PASSWD, and sends the
                            //   following response:
                            //
                            //                        +----+--------+
                            //                        |VER | STATUS |
                            //                        +----+--------+
                            //                        | 1  |   1    |
                            //                        +----+--------+

                            // A STATUS field of X'00' indicates success. If the server returns a
                            // `failure' (STATUS value other than X'00') status, it MUST close the
                            // connection.
                            bool success = buffer[1] == 0;

                            if (count != 2)
                                throw new Exception($"SOCKS Proxy - Expected read count: 2! buffer: {buffer.AsBuffer(count)}");
                            else if (!success)
                                throw new Exception("SOCKS proxy: username+password authentication failed!");

                            HTTPManager.Logger.Information("SOCKSProxy", "Authenticated!", this._parameters.context);

                            // Send connect
                            SendConnect();
                            break;
                        }

                    case NegotiationStates.ConnectResponse:
                        {
                            if (this.ContentProvider.Length < 10)
                                return;

                            var buffer = BufferPool.Get(512, true);
                            var count = this._parameters.stream.Read(buffer, 0, buffer.Length);

                            if (HTTPManager.Logger.IsDiagnostic)
                                HTTPManager.Logger.Information("SOCKSProxy", $"Connect response - buffer: {buffer.AsBuffer(count)} ", this._parameters.context);

                            //   The SOCKS request information is sent by the client as soon as it has
                            //   established a connection to the SOCKS server, and completed the
                            //   authentication negotiations.  The server evaluates the request, and
                            //   returns a reply formed as follows:
                            //
                            //        +----+-----+-------+------+----------+----------+
                            //        |VER | REP |  RSV  | ATYP | BND.ADDR | BND.PORT |
                            //        +----+-----+-------+------+----------+----------+
                            //        | 1  |  1  | X'00' |  1   | Variable |    2     |
                            //        +----+-----+-------+------+----------+----------+
                            //
                            //     Where:
                            //          o  VER    protocol version: X'05'
                            //          o  REP    Reply field:
                            //             o  X'00' succeeded
                            //             o  X'01' general SOCKS server failure
                            //             o  X'02' connection not allowed by ruleset
                            //             o  X'03' Network unreachable
                            //             o  X'04' Host unreachable
                            //             o  X'05' Connection refused
                            //             o  X'06' TTL expired
                            //             o  X'07' Command not supported
                            //             o  X'08' Address type not supported
                            //             o  X'09' to X'FF' unassigned
                            //          o  RSV    RESERVED
                            //          o  ATYP   address type of following address
                            //             o  IP V4 address: X'01'
                            //             o  DOMAINNAME: X'03'
                            //             o  IP V6 address: X'04'
                            //          o  BND.ADDR       server bound address
                            //          o  BND.PORT       server bound port in network octet order
                            //
                            //   Fields marked RESERVED (RSV) must be set to X'00'.

                            SOCKSVersions version = (SOCKSVersions)buffer[0];
                            SOCKSReplies reply = (SOCKSReplies)buffer[1];

                            // at least 10 bytes expected as a result
                            if (count < 10)
                                throw new Exception($"SOCKS proxy: not enough data returned by the server. Expected count is at least 10 bytes, server returned {count} bytes! content: {buffer.AsBuffer(count)}");
                            else if (reply != SOCKSReplies.Succeeded)
                                throw new Exception("SOCKS proxy error: " + reply.ToString());

                            HTTPManager.Logger.Information("SOCKSProxy", "Connected!", this._parameters.context);

                            CallOnSuccess();
                            break;
                        }
                }
            }
            catch(Exception ex)
            {
                CallOnError(ex);
            }
        }

        void CallOnError(Exception ex)
        {
            var callback = Interlocked.Exchange(ref this._parameters.OnError, null);
            Interlocked.Exchange(ref this._parameters.OnSuccess, null);

            this.ContentProvider.Unbind();
            callback?.Invoke(this._parameters, ex, false);
        }

        void CallOnSuccess()
        {
            var callback = Interlocked.Exchange(ref this._parameters.OnSuccess, null);
            Interlocked.Exchange(ref this._parameters.OnError, null);

            this.ContentProvider.Unbind();

            callback?.Invoke(this._parameters);
        }

        private void WriteString(byte[] buffer, ref int count, string str)
        {
            // Get the bytes
            int byteCount = Encoding.UTF8.GetByteCount(str);
            if (byteCount > 255)
                throw new Exception(string.Format("SOCKS Proxy - String is too large ({0}) to fit in 255 bytes!", byteCount.ToString()));

            // number of bytes
            buffer[count++] = (byte)byteCount;

            // and the bytes itself
            Encoding.UTF8.GetBytes(str, 0, str.Length, buffer, count);

            count += byteCount;
        }

        private void WriteBytes(byte[] buffer, ref int count, byte[] bytes)
        {
            Array.Copy(bytes, 0, buffer, count, bytes.Length);
            count += bytes.Length;
        }
    }
}
#endif
