using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
		/// Requests a new texture to either be downloaded from <paramref name="url"/>,
		/// or be retrieved from the local cache.
		/// </summary>
		/// <returns>A handle ID that can be used in <see cref="CancelRequest"/></returns>
		int RequestTexture(string url, Action<Texture2D> success, Action error);

		/// <summary>
		/// Cancels a texture request. After this is called, the request callbacks
		/// will never be called.
		/// </summary>
		void CancelRequest(int handle);
	}

	public class RemoteTextureService : IRemoteTextureService
	{
		private const string TextureHashesKey = "RemoteTextureService.Hashes";
		private const string FileUriPrefix = "file://";
		private const int TexturesToKeep = 20;
		private string TexturesFolder = Path.Combine(Application.persistentDataPath, "RemoteTextures");

		private readonly ICoroutineService _coroutineService;

		private int _handle;
		private readonly Dictionary<int, Coroutine> _requests = new();
		private readonly List<string> _cachedTextures = new();

		public RemoteTextureService(ICoroutineService coroutineService)
		{
			_coroutineService = coroutineService;

			if (PlayerPrefs.HasKey(TextureHashesKey))
			{
				var hashes = PlayerPrefs.GetString(TextureHashesKey).Split(";");
				foreach (var hash in hashes)
				{
					_cachedTextures.Add(hash);
				}
			}

			if (!Directory.Exists(TexturesFolder))
			{
				Directory.CreateDirectory(TexturesFolder);
			}
		}

		public int RequestTexture(string url, Action<Texture2D> callback, Action error)
		{
			var handle = _handle++;

			var downloadRequest = LoadImage(GetImageUri(url), callback, error, handle);
			var coroutine = _coroutineService.StartCoroutine(downloadRequest);
			_requests.Add(handle, coroutine);

			return handle;
		}

		public void CancelRequest(int handle)
		{
			if (_requests.ContainsKey(handle))
			{
				_coroutineService.StopCoroutine(_requests[handle]);
				_requests.Remove(handle);
			}
		}

		private IEnumerator LoadImage(string uri, Action<Texture2D> callback, Action error, int handle)
		{
			var request = UnityWebRequestTexture.GetTexture(uri);
			yield return request.SendWebRequest();

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
				error();
			}
			else
			{
				if (!uri.StartsWith(FileUriPrefix))
				{
					CacheTexture(uri, request.downloadHandler.data);
				}

				callback(((DownloadHandlerTexture) request.downloadHandler).texture);
			}
		}

		// TODO: This should be async
		private void CacheTexture(string uri, byte[] data)
		{
			var hash = GetHashString(uri);
			File.WriteAllBytes(Path.Combine(TexturesFolder, hash), data);

			_cachedTextures.Add(hash);

			while (_cachedTextures.Count > TexturesToKeep)
			{
				File.Delete(Path.Combine(TexturesFolder, _cachedTextures[0]));
				_cachedTextures.RemoveAt(0);
			}

			PlayerPrefs.SetString(TextureHashesKey, string.Join(';', _cachedTextures.ToArray()));
			PlayerPrefs.Save();
		}

		private string GetImageUri(string url)
		{
			var hash = GetHashString(url);

			if (_cachedTextures.Contains(hash))
			{
				return GetLocalImageUri(hash);
			}

			return url;
		}

		private string GetLocalImageUri(string hash)
		{
			return "file://" + Path.Combine(TexturesFolder, hash);
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