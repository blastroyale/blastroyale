#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

namespace Best.HTTP.Hosts.Connections.WebGL
{
    delegate void OnWebGLXHRRequestHandlerDelegate(int nativeId, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 2)] byte[] pBuffer, int length);
    delegate void OnWebGLXHRBufferDelegate(int nativeId, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 2)] byte[] pBuffer, int length);
    delegate void OnWebGLXHRProgressDelegate(int nativeId, int downloaded, int total);
    delegate void OnWebGLXHRErrorDelegate(int nativeId, string error);
    delegate void OnWebGLXHRTimeoutDelegate(int nativeId);
    delegate void OnWebGLXHRAbortedDelegate(int nativeId);
    delegate IntPtr OnWebGLXHRAllocArray(int nativeId, int size);

    internal static class WebGLXHRNativeInterface
    {
        [DllImport("__Internal")]
        public static extern int XHR_Create(string method, string url, string userName, string passwd, int withCredentials);

        /// <summary>
        /// Is an unsigned long representing the number of milliseconds a request can take before automatically being terminated. A value of 0 (which is the default) means there is no timeout.
        /// </summary>
        [DllImport("__Internal")]
        public static extern void XHR_SetTimeout(int nativeId, uint timeout);

        [DllImport("__Internal")]
        public static extern void XHR_SetRequestHeader(int nativeId, string header, string value);

        [DllImport("__Internal")]
        public static extern void XHR_SetResponseHandler(int nativeId,
            OnWebGLXHRRequestHandlerDelegate onresponse,
            OnWebGLXHRErrorDelegate onerror,
            OnWebGLXHRTimeoutDelegate ontimeout,
            OnWebGLXHRAbortedDelegate onabort,
            OnWebGLXHRBufferDelegate onbuffer,
            OnWebGLXHRAllocArray onallocArray);

        [DllImport("__Internal")]
        public static extern void XHR_SetProgressHandler(int nativeId, OnWebGLXHRProgressDelegate onDownloadProgress, OnWebGLXHRProgressDelegate onUploadProgress);

        [DllImport("__Internal")]
        public static extern void XHR_Send(int nativeId, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 2)] byte[] body, int length);

        [DllImport("__Internal")]
        public static extern void XHR_Abort(int nativeId);

        [DllImport("__Internal")]
        public static extern void XHR_Release(int nativeId);

        [DllImport("__Internal")]
        public static extern void XHR_SetLoglevel(int logLevel);
    }
}
#endif
