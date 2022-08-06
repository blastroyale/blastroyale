using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This view is responsible to show the item icons and information
	/// </summary>
	public class EquipmentIconItemView : MonoBehaviour
	{
		[SerializeField, Required] private RawImage _iconImage;
		[SerializeField, Required] private GameObject _loadingView;

		private IMainMenuServices _mainMenuServices;
		private IGameDataProvider _gameDataProvider;

		private int _textureRequestHandle = -1;
		private UniqueId _loadedId = UniqueId.Invalid;

		private void Awake()
		{
			_mainMenuServices = MainInstaller.Resolve<IMainMenuServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		/// <summary>
		/// Sets the information for this view
		/// </summary>
		public void SetInfo(UniqueId uniqueId, Equipment equipment)
		{
			if (_textureRequestHandle >= 0)
			{
				_mainMenuServices.RemoteTextureService.CancelRequest(_textureRequestHandle);
			}

			if (_loadedId != uniqueId)
			{
				_loadedId = uniqueId;
				var url = _gameDataProvider.EquipmentDataProvider.GetInfo(uniqueId).CardUrl;

				_iconImage.gameObject.SetActive(false);
				_textureRequestHandle =
					_mainMenuServices.RemoteTextureService.RequestTexture(url, OnTextureReceived, OnTextureReceived);
			}
		}

		private void OnTextureReceived(Texture2D tex)
		{
			if (_iconImage == null) return;

			_iconImage.texture = tex;

			_loadingView.SetActive(false);
			_iconImage.gameObject.SetActive(true);

			_textureRequestHandle = -1;
		}
	}
}