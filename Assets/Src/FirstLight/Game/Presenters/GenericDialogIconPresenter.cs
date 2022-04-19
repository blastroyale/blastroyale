using System;
using FirstLight.Game.Services;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <inheritdoc />
	/// <remarks>
	/// It adds extra information with a sprite icon in a central location.
	/// </remarks>
	public class GenericDialogIconPresenter : GenericDialogPresenterBase
	{
		[SerializeField] private Image _icon;
		[SerializeField] private TextMeshProUGUI _iconText;
		
		public Transform IconPosition => _icon.transform;
		
		private Vector2 _defaultIconSize;

		/// <summary>
		/// Shows the Generic Icon Dialog PopUp with the necessary information.
		/// If the given <paramref name="showCloseButton"/> is true, then will show the close button icon on the dialog.
		/// Optionally if defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		public async void SetInfo<TId>(string title, string descriptionText, TId id, bool showCloseButton, 
		                          GenericDialogButton button, Action closeCallback = null)
			where TId : struct, Enum
		{
			SetBaseInfo(title, showCloseButton, button, closeCallback);
			_iconText.text = descriptionText;
			var sprite = await Services.AssetResolverService.RequestAsset<TId, Sprite>(id);

			_icon.rectTransform.sizeDelta = sprite.rect.size.sqrMagnitude > _defaultIconSize.sqrMagnitude
				                                ? sprite.rect.size
				                                : _defaultIconSize;

			_icon.sprite = sprite;
		}

		protected override void OnAwake()
		{
			_defaultIconSize = _icon.rectTransform.sizeDelta;
		}
	}
}

