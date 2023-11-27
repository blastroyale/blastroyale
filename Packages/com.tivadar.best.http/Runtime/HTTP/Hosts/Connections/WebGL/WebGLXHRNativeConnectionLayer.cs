#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.HTTP.Hosts.Connections.WebGL
{
    internal static class WebGLXHRNativeConnectionLayer
    {
        static Dictionary<int, WebGLXHRConnection> Connections = new Dictionary<int, WebGLXHRConnection>(1);

        public static void Add(int nativeId, WebGLXHRConnection connection) => Connections.Add(nativeId, connection);
        public static void Remove(int nativeId) => Connections.Remove(nativeId);

        public static void SetupHandlers(int nativeId, HTTPRequest request)
        {
            WebGLXHRNativeInterface.XHR_SetResponseHandler(nativeId, OnResponse, OnError, OnTimeout, OnAborted, OnBufferCallback, OnAllocArray);

            // Setting OnUploadProgress result in an addEventListener("progress", ...) call making the request non-simple.
            // https://forum.unity.com/threads/best-http-released.200006/page-49#post-3696220
            WebGLXHRNativeInterface.XHR_SetProgressHandler(nativeId,
                                   request.DownloadSettings.OnDownloadProgress == null ? (OnWebGLXHRProgressDelegate)null : OnDownloadProgress,
                                   request.UploadSettings.OnUploadProgress == null ? (OnWebGLXHRProgressDelegate)null : OnUploadProgress);
        }

        [AOT.MonoPInvokeCallback(typeof(OnWebGLXHRAllocArray))]
        static unsafe IntPtr OnAllocArray(int nativeId, int length)
        {
            byte[] buffer = BufferPool.Get(length, true);
            fixed (byte* ptr = buffer)
            {
                var p = (IntPtr)ptr;

                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(WebGLXHRConnection), $"({p}) <= OnAllocArray({nativeId}, {length})");

                return p;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(OnWebGLXHRRequestHandlerDelegate))]
        static void OnResponse(int nativeId, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 2)] byte[] pBuffer, int length)
        {
            WebGLXHRConnection conn = null;
            if (!Connections.TryGetValue(nativeId, out conn))
            {
                HTTPManager.Logger.Error("WebGLConnection", $"OnResponse({nativeId}, {pBuffer}, {length}): No WebGL connection found for nativeId({nativeId})!");
                return;
            }

            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose("WebGLConnection", $"OnResponse({nativeId}, {pBuffer}, {length})", conn.Context);

            BufferSegment payload = BufferSegment.Empty;
            if (length > 0)
                payload = pBuffer.AsBuffer(length);

            conn.OnResponse(payload);
        }

        [AOT.MonoPInvokeCallback(typeof(OnWebGLXHRBufferDelegate))]
        public static void OnBufferCallback(int nativeId, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 2)] byte[] pBuffer, int length)
        {
            WebGLXHRConnection conn = null;
            if (!Connections.TryGetValue(nativeId, out conn))
            {
                HTTPManager.Logger.Error("WebGLConnection - OnBufferCallback", "No WebGL connection found for nativeId: " + nativeId.ToString());
                return;
            }

            BufferSegment payload = BufferSegment.Empty;
            if (length > 0)
                payload = pBuffer.AsBuffer(length);

            conn.OnBuffer(payload);
        }

        [AOT.MonoPInvokeCallback(typeof(OnWebGLXHRProgressDelegate))]
        static void OnDownloadProgress(int nativeId, int downloaded, int total)
        {
            WebGLXHRConnection conn = null;
            if (!Connections.TryGetValue(nativeId, out conn))
            {
                HTTPManager.Logger.Error("WebGLConnection - OnDownloadProgress", "No WebGL connection found for nativeId: " + nativeId.ToString());
                return;
            }

            HTTPManager.Logger.Information(nativeId + " OnDownloadProgress", downloaded.ToString() + " / " + total.ToString(), conn.Context);

            conn.OnDownloadProgress(downloaded, total);
        }

        [AOT.MonoPInvokeCallback(typeof(OnWebGLXHRProgressDelegate))]
        static void OnUploadProgress(int nativeId, int uploaded, int total)
        {
            WebGLXHRConnection conn = null;
            if (!Connections.TryGetValue(nativeId, out conn))
            {
                HTTPManager.Logger.Error("WebGLConnection - OnUploadProgress", "No WebGL connection found for nativeId: " + nativeId.ToString());
                return;
            }

            HTTPManager.Logger.Information(nativeId + " OnUploadProgress", uploaded.ToString() + " / " + total.ToString(), conn.Context);

            conn.OnUploadProgress(uploaded, total);
        }

        [AOT.MonoPInvokeCallback(typeof(OnWebGLXHRErrorDelegate))]
        static void OnError(int nativeId, string error)
        {
            WebGLXHRConnection conn = null;
            if (!Connections.TryGetValue(nativeId, out conn))
            {
                HTTPManager.Logger.Error("WebGLConnection - OnError", "No WebGL connection found for nativeId: " + nativeId.ToString() + " Error: " + error);
                return;
            }

            conn.OnError(error);
        }

        [AOT.MonoPInvokeCallback(typeof(OnWebGLXHRTimeoutDelegate))]
        static void OnTimeout(int nativeId)
        {
            WebGLXHRConnection conn = null;
            if (!Connections.TryGetValue(nativeId, out conn))
            {
                HTTPManager.Logger.Error("WebGLConnection - OnTimeout", "No WebGL connection found for nativeId: " + nativeId.ToString());
                return;
            }

            conn.OnTimeout();
        }

        [AOT.MonoPInvokeCallback(typeof(OnWebGLXHRAbortedDelegate))]
        static void OnAborted(int nativeId)
        {
            WebGLXHRConnection conn = null;
            if (!Connections.TryGetValue(nativeId, out conn))
            {
                HTTPManager.Logger.Error("WebGLConnection - OnAborted", "No WebGL connection found for nativeId: " + nativeId.ToString());
                return;
            }

            conn.OnAborted();
        }

    }
}
#endif
