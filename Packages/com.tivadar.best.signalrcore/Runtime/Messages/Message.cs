using System;

namespace Best.SignalR.Messages
{
    /// <summary>
    /// Possible message types the SignalR protocol can use.
    /// </summary>
    public enum MessageTypes : int
    {
        /// <summary>
        /// This is a made up message type, for easier handshake handling.
        /// </summary>
        Handshake  = 0,

        /// <summary>
        /// Represents the Invocation message type.
        /// <see href="https://github.com/dotnet/aspnetcore/blob/master/src/SignalR/docs/specs/HubProtocol.md#invocation-message-encoding"/>
        /// </summary>
        Invocation = 1,

        /// <summary>
        /// Represents the StreamItem message type.
        /// <see href="https://github.com/dotnet/aspnetcore/blob/master/src/SignalR/docs/specs/HubProtocol.md#streamitem-message-encoding"/>
        /// </summary>
        StreamItem = 2,

        /// <summary>
        /// Represents the Completion message type.
        /// <see href="https://github.com/dotnet/aspnetcore/blob/master/src/SignalR/docs/specs/HubProtocol.md#completion-message-encoding"/>
        /// </summary>
        Completion = 3,

        /// <summary>
        /// Represents the StreamInvocation message type.
        /// <see href="https://github.com/dotnet/aspnetcore/blob/master/src/SignalR/docs/specs/HubProtocol.md#streaminvocation-message-encoding"/>
        /// </summary>
        StreamInvocation = 4,

        /// <summary>
        /// Represents the CancelInvocation message type.
        /// <see href="https://github.com/dotnet/aspnetcore/blob/master/src/SignalR/docs/specs/HubProtocol.md#cancelinvocation-message-encoding"/>
        /// </summary>
        CancelInvocation = 5,

        /// <summary>
        /// Represents the Ping message type.
        /// <see href="https://github.com/dotnet/aspnetcore/blob/master/src/SignalR/docs/specs/HubProtocol.md#ping-message-encoding"/>
        /// </summary>
        Ping = 6,

        /// <summary>
        /// Represents the Close message type.
        /// <see href="https://github.com/dotnet/aspnetcore/blob/master/src/SignalR/docs/specs/HubProtocol.md#close-message-encoding"/>
        /// </summary>
        Close = 7
    }

    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
    public struct Message
    {
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public MessageTypes type;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string invocationId;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public bool nonblocking;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string target;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public object[] arguments;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string[] streamIds;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public object item;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public object result;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string error;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public bool allowReconnect;

        public override string ToString()
        {
            switch (this.type)
            {
                case MessageTypes.Handshake:
                    return string.Format("[Handshake Error: '{0}'", this.error);
                case MessageTypes.Invocation:
                    return string.Format("[Invocation Id: {0}, Target: '{1}', Argument count: {2}, Stream Ids: {3}]", this.invocationId, this.target, this.arguments != null ? this.arguments.Length : 0, this.streamIds != null ? this.streamIds.Length : 0);
                case MessageTypes.StreamItem:
                    return string.Format("[StreamItem Id: {0}, Item: {1}]", this.invocationId, this.item.ToString());
                case MessageTypes.Completion:
                    return string.Format("[Completion Id: {0}, Result: {1}, Error: '{2}']", this.invocationId, this.result, this.error);
                case MessageTypes.StreamInvocation:
                    return string.Format("[StreamInvocation Id: {0}, Target: '{1}', Argument count: {2}]", this.invocationId, this.target, this.arguments != null ? this.arguments.Length : 0);
                case MessageTypes.CancelInvocation:
                    return string.Format("[CancelInvocation Id: {0}]", this.invocationId);
                case MessageTypes.Ping:
                    return "[Ping]";
                case MessageTypes.Close:
                    return string.IsNullOrEmpty(this.error) ?
                        string.Format("[Close allowReconnect: {0}]", this.allowReconnect) :
                        string.Format("[Close Error: '{0}', allowReconnect: {1}]", this.error, this.allowReconnect);
                default:
                    return "Unknown message! Type: " + this.type;
            }
        }
    }
}
