using System;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// Handles loading remote textures with a loading spinner / error states.
	/// </summary>
	public class RemoteTextureView : MonoBehaviour
	{
		[SerializeField, Required] private LoadingSpinnerView _loadingSpinner;
		[SerializeField, Required] private Image _errorSprite;
		[SerializeField, Required] private RawImage _image;

		private IMainMenuServices _mainMenuServices;

		private string _currentUrl;
		private int _textureRequestHandle = -1;
		private bool _loadedSuccessfully;

		private void Awake()
		{
			_mainMenuServices = MainInstaller.Resolve<IMainMenuServices>();
		}

		private void OnDestroy()
		{
			if (_textureRequestHandle >= 0)
			{
				_mainMenuServices.RemoteTextureService.CancelRequest(_textureRequestHandle);
			}
		}

		/// <summary>
		/// Loads the provided url, and properly handles multiple calls to the method.
		/// </summary>
		public void LoadImage(string url)
		{
			if (url == null)
			{
				_currentUrl = null;
				OnError();
				return;
			}
			
			// Do nothing if we're already loading this url or if it's successfully loaded
			if (_currentUrl == url && (_textureRequestHandle >= 0 || _loadedSuccessfully)) return;

			_currentUrl = url;
			_loadedSuccessfully = false;

			_loadingSpinner.gameObject.SetActive(true);
			_errorSprite.gameObject.SetActive(false);
			_image.gameObject.SetActive(false);

			if (_textureRequestHandle >= 0)
			{
				_mainMenuServices.RemoteTextureService.CancelRequest(_textureRequestHandle);
			}

			_textureRequestHandle = _mainMenuServices.RemoteTextureService.RequestTexture(url, OnSuccess, OnError);
		}

		private void OnSuccess(Texture2D tex)
		{
			_image.texture = tex;

			_image.gameObject.SetActive(true);
			_loadingSpinner.gameObject.SetActive(false);

			_textureRequestHandle = -1;
			_loadedSuccessfully = true;
		}

		private void OnError()
		{
			_loadingSpinner.gameObject.SetActive(false);
			_errorSprite.gameObject.SetActive(true);

			_textureRequestHandle = -1;
			_loadedSuccessfully = false;
		}
	}
}