using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

using Best.HTTP.Shared;

using UnityEngine;

#if WITH_UNITASK
using Cysharp.Threading.Tasks;
#endif

namespace Best.HTTP
{
    /// <summary>
    /// Represents an exception thrown during or as a result of a Task-based asynchronous HTTP operations.
    /// </summary>
    public sealed class AsyncHTTPException : Exception
    {
        /// <summary>
        /// Gets the status code of the server's response.
        /// </summary>
        public readonly int StatusCode;

        /// <summary>
        /// Gets the content sent by the server. This is usually an error page for 4xx or 5xx responses.
        /// </summary>
        public readonly string Content;

        public AsyncHTTPException(string message)
            : base(message)
        {
        }

        public AsyncHTTPException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public AsyncHTTPException(int statusCode, string message, string content)
            :base(message)
        {
            this.StatusCode = statusCode;
            this.Content = content;
        }

        public override string ToString()
        {
            return string.Format("StatusCode: {0}, Message: {1}, Content: {2}, StackTrace: {3}", this.StatusCode, this.Message, this.Content, this.StackTrace);
        }
    }

    /// <summary>
    /// A collection of extension methods for working with HTTP requests asynchronously using <see cref="Task{TResult}"/>.
    /// </summary>
    public static class HTTPRequestAsyncExtensions
    {
        /// <summary>
        /// Asynchronously sends an HTTP request and retrieves the response as an <see cref="AssetBundle"/>.
        /// </summary>
        /// <param name="request">The <see cref="HTTPRequest"/> to send.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>
        /// A Task that represents the asynchronous operation. The Task will complete with the retrieved AssetBundle
        /// if the request succeeds. If the request fails or is canceled, the Task will complete with an exception.
        /// </returns>
#if WITH_UNITASK
        public static UniTask<AssetBundle> GetAssetBundleAsync(this HTTPRequest request, CancellationToken token = default)
#else
        public static Task<AssetBundle> GetAssetBundleAsync(this HTTPRequest request, CancellationToken token = default)
#endif
        {
            return CreateTask<AssetBundle>(request, token,
#if UNITY_2023_1_OR_NEWER
                async
#endif
                (req, resp, tcs) =>
            {
                switch (req.State)
                {
                    // The request finished without any problem.
                    case HTTPRequestStates.Finished:
                        if (resp.IsSuccess)
                        {
                            var bundleLoadOp = AssetBundle.LoadFromMemoryAsync(resp.Data);

#if !UNITY_2023_1_OR_NEWER
                            HTTPUpdateDelegator.Instance.StartCoroutine(BundleLoader(bundleLoadOp, tcs));
#else
                            await Awaitable.FromAsyncOperation(bundleLoadOp);

                            tcs.TrySetResult(bundleLoadOp.assetBundle);
#endif
                        }
                        else
                            GenericResponseHandler<AssetBundle>(req, resp, tcs);
                        break;

                    default:
                        GenericResponseHandler<AssetBundle>(req, resp, tcs);
                        break;
                }
            });
        }

#if !UNITY_2023_1_OR_NEWER
#if WITH_UNITASK
        static IEnumerator BundleLoader(AssetBundleCreateRequest req, UniTaskCompletionSource<AssetBundle> tcs)
#else
        static IEnumerator BundleLoader(AssetBundleCreateRequest req, TaskCompletionSource<AssetBundle> tcs)
#endif
        {
            yield return req;

            tcs.TrySetResult(req.assetBundle);
        }
#endif

        /// <summary>
        /// Asynchronously sends an HTTP request and retrieves the raw <see cref="HTTPResponse"/>.
        /// </summary>
        /// <remarks>
        /// This method is particularly useful when you want to access the raw response without any specific processing 
        /// like converting the data into a string, texture, or other formats. It provides flexibility in handling 
        /// the response for custom or advanced use cases.
        /// </remarks>
        /// <param name="request">The <see cref="HTTPRequest"/> to send.</param>
        /// <param name="token">An optional <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that represents the asynchronous operation. The value of TResult is the raw <see cref="HTTPResponse"/>.
        /// If the request completes successfully, the task will return the HTTPResponse. If there's an error during the request or if 
        /// the request gets canceled, the task will throw an exception, which can be caught and processed by the calling method.
        /// </returns>
        /// <exception cref="AsyncHTTPException">Thrown if there's an error in the request or if the server returns an error status code.</exception>
#if WITH_UNITASK
        public static UniTask<HTTPResponse> GetHTTPResponseAsync(this HTTPRequest request, CancellationToken token = default)
#else
        public static Task<HTTPResponse> GetHTTPResponseAsync(this HTTPRequest request, CancellationToken token = default)
#endif
        {
            return CreateTask<HTTPResponse>(request, token, (req, resp, tcs) =>
            {
                switch (req.State)
                {
                    // The request finished without any problem.
                    case HTTPRequestStates.Finished:
                        tcs.TrySetResult(resp);
                        break;

                    default:
                        GenericResponseHandler<HTTPResponse>(req, resp, tcs);
                        break;
                }
            });
        }

        /// <summary>
        /// Asynchronously sends an <see cref="HTTPRequest"/> and retrieves the response content as a <c>string</c>.
        /// </summary>
        /// <param name="request">The <see cref="HTTPRequest"/> to send.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>
        /// A Task that represents the asynchronous operation. The Task will complete with the retrieved <c>string</c> content
        /// if the request succeeds. If the request fails or is canceled, the Task will complete with an exception.
        /// </returns>
#if WITH_UNITASK
        public static UniTask<string> GetAsStringAsync(this HTTPRequest request, CancellationToken token = default)
#else
        public static Task<string> GetAsStringAsync(this HTTPRequest request, CancellationToken token = default)
#endif
        {
            return CreateTask<string>(request, token, (req, resp, tcs) =>
            {
                switch (req.State)
                {
                    // The request finished without any problem.
                    case HTTPRequestStates.Finished:
                        if (resp.IsSuccess)
                            tcs.TrySetResult(resp.DataAsText);
                        else
                            GenericResponseHandler<string>(req, resp, tcs);
                        break;

                    default:
                        GenericResponseHandler<string>(req, resp, tcs);
                        break;
                }
            });
        }

        /// <summary>
        /// Asynchronously sends an <see cref="HTTPRequest"/> and retrieves the response content as a <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="request">The <see cref="HTTPRequest"/> to send.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>
        /// A Task that represents the asynchronous operation. The Task will complete with the retrieved <see cref="Texture2D"/>
        /// if the request succeeds. If the request fails or is canceled, the Task will complete with an exception.
        /// </returns>
#if WITH_UNITASK
        public static UniTask<Texture2D> GetAsTexture2DAsync(this HTTPRequest request, CancellationToken token = default)
#else
        public static Task<Texture2D> GetAsTexture2DAsync(this HTTPRequest request, CancellationToken token = default)
#endif
        {
            return CreateTask<Texture2D>(request, token, (req, resp, tcs) =>
            {
                switch (req.State)
                {
                    // The request finished without any problem.
                    case HTTPRequestStates.Finished:
                        if (resp.IsSuccess)
                            tcs.TrySetResult(resp.DataAsTexture2D);

                        else
                            GenericResponseHandler<Texture2D>(req, resp, tcs);
                        break;

                    default:
                        GenericResponseHandler<Texture2D>(req, resp, tcs);
                        break;
                }
            });
        }

        /// <summary>
        /// Asynchronously sends an <see cref="HTTPRequest"/> and retrieves the response content as a <c>byte[]</c>.
        /// </summary>
        /// <param name="request">The <see cref="HTTPRequest"/> to send.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>
        /// A Task that represents the asynchronous operation. The Task will complete with the retrieved <c>byte[]</c>
        /// if the request succeeds. If the request fails or is canceled, the Task will complete with an exception.
        /// </returns>
#if WITH_UNITASK
        public static UniTask<byte[]> GetRawDataAsync(this HTTPRequest request, CancellationToken token = default)
#else
        public static Task<byte[]> GetRawDataAsync(this HTTPRequest request, CancellationToken token =  default)
#endif
        {
            return CreateTask<byte[]>(request, token, (req, resp, tcs) =>
            {
                switch (req.State)
                {
                    // The request finished without any problem.
                    case HTTPRequestStates.Finished:
                        if (resp.IsSuccess)
                            tcs.TrySetResult(resp.Data);
                        else
                            GenericResponseHandler<byte[]>(req, resp, tcs);
                        break;

                    default:
                        GenericResponseHandler<byte[]>(req, resp, tcs);
                        break;
                }
            });
        }

        /// <summary>
        /// Asynchronously sends an <see cref="HTTPRequest"/> and deserializes the response content into an object of type T using JSON deserialization.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the JSON content into.</typeparam>
        /// <param name="request">The <see cref="HTTPRequest"/> to send.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>
        /// A Task that represents the asynchronous operation. The Task will complete with the deserialized object
        /// if the request succeeds and the response content can be deserialized. If the request fails, is canceled, or
        /// the response cannot be deserialized, the Task will complete with an exception.
        /// </returns>
#if WITH_UNITASK
        public static UniTask<T> GetFromJsonResultAsync<T>(this HTTPRequest request, CancellationToken token = default)
#else
        public static Task<T> GetFromJsonResultAsync<T>(this HTTPRequest request, CancellationToken token = default)
#endif
        {
            return HTTPRequestAsyncExtensions.CreateTask<T>(request, token, (req, resp, tcs) =>
            {
                switch (req.State)
                {
                    // The request finished without any problem.
                    case HTTPRequestStates.Finished:
                        if (resp.IsSuccess)
                        {
                            try
                            {
                                tcs.TrySetResult(Best.HTTP.JSON.LitJson.JsonMapper.ToObject<T>(resp.DataAsText));
                            }
                            catch (Exception ex)
                            {
                                tcs.TrySetException(ex);
                            }
                        }
                        else
                            GenericResponseHandler<T>(req, resp, tcs);
                        break;

                    default:
                        GenericResponseHandler<T>(req, resp, tcs);
                        break;
                }
            });
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#if WITH_UNITASK
        public static UniTask<T> CreateTask<T>(HTTPRequest request, CancellationToken token, Action<HTTPRequest, HTTPResponse, UniTaskCompletionSource<T>> callback)
#else
        public static Task<T> CreateTask<T>(HTTPRequest request, CancellationToken token, Action<HTTPRequest, HTTPResponse, TaskCompletionSource<T>> callback)
#endif
        {
            HTTPManager.Setup();

#if WITH_UNITASK
            var tcs = new UniTaskCompletionSource<T>();
#else
            var tcs = new TaskCompletionSource<T>();
#endif

            request.Callback = (req, resp) =>
            {
                if (token.IsCancellationRequested)
                    tcs.TrySetCanceled();
                else
                    callback(req, resp, tcs);
            };

            if (token.CanBeCanceled)
                token.Register((state) => (state as HTTPRequest)?.Abort(), request);

            if (request.State == HTTPRequestStates.Initial)
                request.Send();

            return tcs.Task;
        }

#if WITH_UNITASK
        private static void GenericResponseHandler<T>(HTTPRequest req, HTTPResponse resp, UniTaskCompletionSource<T> tcs)
#else
        private static void GenericResponseHandler<T>(HTTPRequest req, HTTPResponse resp, TaskCompletionSource<T> tcs)
#endif
        {
            switch (req.State)
            {
                // The request finished without any problem.
                case HTTPRequestStates.Finished:
                    if (!resp.IsSuccess)
                        tcs.TrySetException(CreateException($"Request finished Successfully, but the server sent an error ({resp.StatusCode} - '{resp.Message}').", resp));
                    break;

                // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
                case HTTPRequestStates.Error:
                    Log(req, $"Request Finished with Error! {req.Exception?.Message} - {req.Exception?.StackTrace}");

                    tcs.TrySetException(CreateException("No Exception", null, req.Exception));
                    break;

                // The request aborted, initiated by the user.
                case HTTPRequestStates.Aborted:
                    Log(req, "Request Aborted!");

                    tcs.TrySetCanceled();
                    break;

                // Connecting to the server is timed out.
                case HTTPRequestStates.ConnectionTimedOut:
                    Log(req, "Connection Timed Out!");

                    tcs.TrySetException(CreateException("Connection Timed Out!"));
                    break;

                // The request didn't finished in the given time.
                case HTTPRequestStates.TimedOut:
                    Log(req, "Processing the request Timed Out!");

                    tcs.TrySetException(CreateException("Processing the request Timed Out!"));
                    break;
            }
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void Log(HTTPRequest request, string str)
        {
            HTTPManager.Logger.Verbose(nameof(HTTPRequestAsyncExtensions), str, request.Context);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static Exception CreateException(string errorMessage, HTTPResponse resp = null, Exception ex = null)
        {
            if (resp != null)
                return new AsyncHTTPException(resp.StatusCode, resp.Message, resp.DataAsText);
            else if (ex != null)
                return new AsyncHTTPException(ex.Message, ex);
            else
                return new AsyncHTTPException(errorMessage);
        }
    }
}
