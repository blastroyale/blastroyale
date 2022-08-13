using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FirstLight.FLogger;
using FirstLight.Services;
using UnityEngine;
using UnityEngine.Networking;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Handles downloading and caching of remote textures.
	/// </summary>
	public interface IRemoteTextureService
	{
		/// <summary>
		/// Sets the texture that's passed when an error occurs.
		/// </summary>
		void SetErrorTexture(Texture2D errorTexture);

		/// <summary>
		/// Requests a new texture to either be downloaded from <paramref name="url"/>,
		/// or be retrieved from the local cache.
		/// </summary>
		/// <returns>A handle ID that can be used in <see cref="CancelRequest"/></returns>
		int RequestTexture(string url, Action<Texture2D> success, Action<Texture2D> error);

		/// <summary>
		/// Cancels a texture request. After this is called, the request callbacks
		/// will never be called.
		/// </summary>
		void CancelRequest(int handle);
	}

	/// <inheritdoc />
	public class RemoteTextureService : IRemoteTextureService
	{
		private const bool FORCE_DOWNLOAD = false; // Use true for debug only

		private const string TEXTURE_HASHES_KEY = "RemoteTextureService.Hashes";
		private const string FILE_URI_PREFIX = "file://";
		private const int TEXTURES_TO_KEEP = 20;
		private readonly string TEXTURES_FOLDER = Path.Combine(Application.persistentDataPath, "RemoteTextures");

		private readonly ICoroutineService _coroutineService;
		private readonly IThreadService _threadService;

		private Texture2D _errorTexture;
		private int _handle;
		private readonly Dictionary<int, Coroutine> _requests = new();
		private readonly List<string> _cachedTextures = new();

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
		public void SetErrorTexture(Texture2D errorTexture)
		{
			_errorTexture = errorTexture;
		}

		/// <inheritdoc />
		public int RequestTexture(string url, Action<Texture2D> callback, Action<Texture2D> error)
		{
			FLog.Info($"Requested texture: {url}");
			
			var handle = _handle++;
			var downloadRequest = LoadImage(GetImageUri(url), callback, error, handle);
			var coroutine = _coroutineService.StartCoroutine(downloadRequest);
			_requests.Add(handle, coroutine);

			return handle;
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

		private IEnumerator LoadImage(string uri, Action<Texture2D> callback, Action<Texture2D> error, int handle)
		{
			FLog.Verbose($"Loading texture URI: {uri}");

			var request = UnityWebRequestTexture.GetTexture(uri);
			yield return request.SendWebRequest();
			
			// TODO: Why not use Task instead? -> while (!request.isDone) await Task.Yield();

			if (_requests.ContainsKey(handle))
			{
				_requests.Remove(handle);
			}
			else
			{
				yield return null;
			}

			if (request.result != UnityWebRequest.Result.Success)
			{
				FLog.Warn($"Error loading texture from {uri}: {request.error}");
				error(_errorTexture);
			}
			else
			{
				var tex = ((DownloadHandlerTexture) request.downloadHandler).texture;
				if (uri.StartsWith(FILE_URI_PREFIX))
				{
					FLog.Verbose($"Loaded texture URI from cache: {uri}");
					callback(tex);
				}
				else
				{
					CacheTexture(tex, request.downloadHandler.data, uri, callback);
				}
			}
		}

		private void CacheTexture(Texture2D tex, byte[] data, string uri, Action<Texture2D> callback)
		{
			_threadService.Enqueue(() =>
			{
				lock (_cachedTextures)
				{
					var hash = GetHashString(uri);
					File.WriteAllBytes(Path.Combine(TEXTURES_FOLDER, hash), data);

					_cachedTextures.Add(hash);

					while (_cachedTextures.Count > TEXTURES_TO_KEEP)
					{
						File.Delete(Path.Combine(TEXTURES_FOLDER, _cachedTextures[0]));
						_cachedTextures.RemoveAt(0);
					}

					// Doesn't matter what we return
					return hash;
				}
			}, _ =>
			{
				PlayerPrefs.SetString(TEXTURE_HASHES_KEY, string.Join(';', _cachedTextures.ToArray()));
				PlayerPrefs.Save();

				FLog.Verbose($"Cached texture URI: {uri}");

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