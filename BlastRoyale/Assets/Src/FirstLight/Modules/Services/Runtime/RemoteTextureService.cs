using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Services;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace FirstLight.Game.Services
{
	public class RemoteTextureException : Exception
	{
		public RemoteTextureException(string message, Exception innerException) : base(message, innerException)
		{
		}

		public RemoteTextureException(string message) : base(message)
		{
		}
	}

	/// <summary>
	/// Handles downloading and caching of remote textures.
	/// </summary>
	public interface IRemoteTextureService
	{
		/// <summary>
		/// Requests a new texture to either be downloaded from <paramref name="url"/>,
		/// or be retrieved from the local cache.
		/// </summary>
		/// <returns>A handle ID that can be used in <see cref="CancelRequest"/></returns>
		int RequestTexture(string url, Action<Texture2D> success, Action error = null, bool cache = true);

		/// <summary>
		/// Requests a new texture to either be downloaded from <paramref name="url"/>,
		/// or be retrieved from the local cache.
		/// You can cancel the request using the cancellation token
		/// </summary>
		UniTask<Texture2D> RequestTexture(string url, bool cacheOnDisk = true, CancellationToken cancellationToken = default);

		/// <summary>
		/// Sets the visual element background to be the remote texture
		/// </summary>
		int SetTexture(VisualElement element, string url, bool cache = true);

		/// <summary>
		/// Cancels a texture request. After this is called, the request callbacks
		/// will never be called.
		/// </summary>
		void CancelRequest(int handle);

		/// <summary>
		/// Deletes all cached downloaded images.
		/// </summary>
		void ClearCache();
	}

	/// <inheritdoc />
	public class RemoteTextureService : IRemoteTextureService
	{
		private const bool FORCE_DOWNLOAD = false; // Use true for debug only

		private const string TEXTURE_HASHES_KEY = "RemoteTextureService.Hashes";
		private const string FILE_URI_PREFIX = "file://";
		private const int TEXTURES_TO_KEEP = 200;
		private readonly string TEXTURES_FOLDER = Path.Combine(Application.persistentDataPath, "RemoteTextures");

		private readonly ICoroutineService _coroutineService;
		private readonly IThreadService _threadService;

		private int _handle;
		private readonly Dictionary<int, Coroutine> _requests = new ();
		private readonly List<string> _cachedTextures = new ();

		public RemoteTextureService(ICoroutineService coroutineService, IThreadService threadService)
		{
			_coroutineService = coroutineService;
			_threadService = threadService;

			if (PlayerPrefs.HasKey(TEXTURE_HASHES_KEY))
			{
				var hashes = PlayerPrefs.GetString(TEXTURE_HASHES_KEY).Split(";");
				foreach (var hash in hashes)
				{
					_cachedTextures.Add(hash);
				}
			}

			if (!Directory.Exists(TEXTURES_FOLDER))
			{
				Directory.CreateDirectory(TEXTURES_FOLDER);
			}
		}

		/// <inheritdoc />
		public int RequestTexture(string url, Action<Texture2D> callback, Action error = null, bool cache = true)
		{
			FLog.Info($"Requested texture: {url}");

			var handle = _handle++;
			var downloadRequest = LoadImage(url, callback, error, handle, cache);
			var coroutine = _coroutineService.StartCoroutine(downloadRequest);
			_requests.Add(handle, coroutine);

			return handle;
		}

		public UniTask<Texture2D> RequestTexture(string url, bool cacheOnDisk = true, CancellationToken cancellationToken = default)
		{
			var utcs = new UniTaskCompletionSource<Texture2D>();
			var handle = _handle++;
			cancellationToken.Register(() =>
			{
				CancelRequest(handle);
				utcs.TrySetCanceled(cancellationToken);
			});
			var request = LoadImage(url, (text) =>
			{
				utcs.TrySetResult(text);
			}, () =>
			{
				utcs.TrySetException(new RemoteTextureException("failed to download texture"));
			}, handle, cacheOnDisk);

			var routine = _coroutineService.StartCoroutine(request);
			_requests.Add(handle, routine);
			return utcs.Task;
		}

		public int SetTexture(VisualElement element, string url, bool cache = true)
		{
			return RequestTexture(url, tex =>
			{
				if (element == null || element.parent == null || element.panel == null) return;
				element.style.backgroundImage = new StyleBackground(tex);
				element.style.display = DisplayStyle.Flex;
			}, null, cache);
		}

		/// <inheritdoc />
		public void CancelRequest(int handle)
		{
			if (_requests.ContainsKey(handle))
			{
				_coroutineService.StopCoroutine(_requests[handle]);
				_requests.Remove(handle);
			}
		}

		/// <inheritdoc />
		public void ClearCache()
		{
			foreach (var textureName in _cachedTextures)
			{
				File.Delete(Path.Combine(TEXTURES_FOLDER, textureName));
			}

			_cachedTextures.Clear();
		}

		private IEnumerator LoadImage(string uri, Action<Texture2D> callback, Action error, int handle, bool cacheOnDisk = true, bool useCache = true)
		{
			FLog.Verbose($"Loading texture URI: {uri}");
			var cachedUri = useCache ? GetImageUri(uri) : uri;
			var request = UnityWebRequestTexture.GetTexture(cachedUri);
			yield return request.SendWebRequest();

			if (_requests.ContainsKey(handle))
			{
				_requests.Remove(handle);
			}
			else
			{
				yield return null;
			}

			// Filt not present in cache anymore
			if (cachedUri.StartsWith(FILE_URI_PREFIX) && request.responseCode == 404)
			{
				lock (_cachedTextures)
				{
					_cachedTextures.Remove(GetHashString(uri));
				}

				FLog.Info("File is not cached anymore: " + cachedUri);
				yield return LoadImage(uri, callback, error, handle, cacheOnDisk, false);
				yield break;
			}

			if (request.result != UnityWebRequest.Result.Success)
			{
				FLog.Error($"Error loading texture from {cachedUri}: {request.error}");
				error?.Invoke();
			}
			else
			{
				var tex = ((DownloadHandlerTexture) request.downloadHandler).texture;
				if (cachedUri.StartsWith(FILE_URI_PREFIX))
				{
					FLog.Info($"Loaded texture URI from cache: {cachedUri}");
					callback(tex);
				}
				else if (cacheOnDisk)
				{
					CacheTexture(tex, request.downloadHandler.data, uri, callback);
				}
			}
		}

		private void CacheTexture(Texture2D tex, byte[] data, string uri, Action<Texture2D> callback)
		{
			var hash = GetHashString(uri);
			_threadService.Enqueue(() =>
			{
				lock (_cachedTextures)
				{
					File.WriteAllBytes(Path.Combine(TEXTURES_FOLDER, hash), data);

					_cachedTextures.Add(hash);

					while (_cachedTextures.Count > TEXTURES_TO_KEEP)
					{
						FLog.Info("Removing cached texture " + _cachedTextures[0] + " because we have " + _cachedTextures.Count);
						File.Delete(Path.Combine(TEXTURES_FOLDER, _cachedTextures[0]));
						_cachedTextures.RemoveAt(0);
					}

					// Doesn't matter what we return
					return hash;
				}
			}, _ =>
			{
				lock (_cachedTextures)
				{
					PlayerPrefs.SetString(TEXTURE_HASHES_KEY, string.Join(';', _cachedTextures.ToArray()));
				}

				PlayerPrefs.Save();
				FLog.Info($"Cached texture URI: {uri} with hash " + hash);

				callback(tex);
			}, ex =>
			{
				// If an error occurs we log it and return the texture as normal
				FLog.Warn($"Error caching texture: {ex.Message}");
				callback(tex);
			});
		}

		private string GetImageUri(string url)
		{
			var hash = GetHashString(url);

			if (!FORCE_DOWNLOAD && _cachedTextures.Contains(hash))
			{
				return GetLocalImageUri(hash);
			}

			return url;
		}

		private string GetLocalImageUri(string hash)
		{
			return "file://" + Path.Combine(TEXTURES_FOLDER, hash);
		}

		private static string GetHashString(string inputString)
		{
			using HashAlgorithm algorithm = MD5.Create();
			var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));

			var sb = new StringBuilder();
			foreach (var b in hash)
			{
				sb.Append(b.ToString("X2"));
			}

			return sb.ToString();
		}
	}
}