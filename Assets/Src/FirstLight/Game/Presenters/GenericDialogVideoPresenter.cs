using System;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Video;

namespace FirstLight.Game.Presenters
{
	/// <inheritdoc />
	/// <remarks>
	/// It adds extra information with a sprite icon in a central location.
	/// </remarks>
	public class GenericDialogVideoPresenter : GenericDialogPresenterBase
	{
		[SerializeField, Required] private TextMeshProUGUI _descriptionText;
		[SerializeField, Required] private VideoPlayer _videoPlayer;
		
		/// <summary>
		/// Shows the Generic Video Dialog PopUp with the necessary information, playing the video on open.
		/// If the given <paramref name="showCloseButton"/> is true, then will show the close button icon on the dialog.
		/// Optionally if defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		public async void SetInfo<TId>(string title, string descriptionText, TId id, bool showCloseButton, 
		                          GenericDialogButton button, Action closeCallback = null)
			where TId : struct, Enum
		{
			var clip = Services.AssetResolverService.RequestAsset<TId, VideoClip>(id);
			
			_descriptionText.text = descriptionText;
			
			SetBaseInfo(title, showCloseButton, button, closeCallback);

			await clip;
			
			if (!this.IsDestroyed())
			{
				_videoPlayer.clip = clip.Result;
			
				_videoPlayer.Play();
			}
		}
		
		protected override void OnClosedCompleted()
		{
			Addressables.Release(_videoPlayer.clip);
			base.OnClosedCompleted();
		}
	}
}

