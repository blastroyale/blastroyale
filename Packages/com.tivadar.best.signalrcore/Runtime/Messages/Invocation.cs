using System;

namespace Best.SignalR.Messages
{
    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
    public struct Completion
    {
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public MessageTypes type;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string invocationId;
    }

    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
    public struct CompletionWithResult
    {
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public MessageTypes type;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string invocationId;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public object result;
    }

    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
    public struct CompletionWithError
    {
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public MessageTypes type;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string invocationId;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string error;
    }

    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
    public struct StreamItemMessage
    {
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public MessageTypes type;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string invocationId;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public object item;
    }

    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
    public struct InvocationMessage
    {
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public MessageTypes type;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string invocationId;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public bool nonblocking;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string target;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public object[] arguments;
    }

    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
    public struct UploadInvocationMessage
    {
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public MessageTypes type;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string invocationId;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public bool nonblocking;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string target;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public object[] arguments;
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string[] streamIds;
    }

    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
    public struct CancelInvocationMessage
    {
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public MessageTypes type { get { return MessageTypes.CancelInvocation; } }
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string invocationId;
    }

    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
    public struct PingMessage
    {
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public MessageTypes type { get { return MessageTypes.Ping; } }
    }

    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
    public struct CloseMessage
    {
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public MessageTypes type { get { return MessageTypes.Close; } }
    }

    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve]
    public struct CloseWithErrorMessage
    {
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public MessageTypes type { get { return MessageTypes.Close; } }
        [Best.HTTP.Shared.PlatformSupport.IL2CPP.Preserve] public string error;
    }
}
