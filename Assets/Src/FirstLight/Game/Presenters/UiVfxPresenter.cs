using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.Game.Views.MainMenuViews;
using FirstLight.Services;
using FirstLight.UiService;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace FirstLight.Game.Presenters
{
	/// /// <summary>
	/// This Presenter handles the VFX for the UI by:
	/// - Player new VFX animations
	/// </summary>
	public class UiVfxPresenter : UiPresenter
	{
		[SerializeField, Required] private UiVfxImage _imageRef;
		[SerializeField, Required] private Transform _defaultFloatingTextTransform;
		[SerializeField, Required] private MainMenuFloatingTextView _floatingTextRef;

		private IObjectPool<UiVfxImage> _imagePool;
		private IObjectPool<MainMenuFloatingTextView> _floatingTextPool;

		private void Awake()
		{
			_imagePool = new GameObjectPool<UiVfxImage>(2, _imageRef);
			_floatingTextPool = new GameObjectPool<MainMenuFloatingTextView>(0, _floatingTextRef);
		}

		/// <summary>
		/// Plays the VFX animation with the necessary information to play the moving animation to the given <paramref name="targetWorldPosition"/>
		/// It will execute the given <paramref name="onCompleteCallback"/> when the VFX ends 
		/// </summary>
		public void PlayAnimation(Sprite sprite, float delay, Vector3 originWorldPosition, Vector3 targetWorldPosition,
			UnityAction onCompleteCallback)
		{
			var image = _imagePool.Spawn();

			image.Play(sprite, delay, originWorldPosition, targetWorldPosition, () =>
			{
				_imagePool.Despawn(image);
				onCompleteCallback?.Invoke();
			});
		}

		/// <summary>
		/// Plays the Floating Text animation with the given <paramref name="string"/>.
		/// </summary>
		public void PlayFloatingText(string text)
		{
			PlayFloatingText(text, _defaultFloatingTextTransform.position);
		}

		/// <summary>
		/// Plays the Floating Text animation with the given <paramref name="string"/> from position <paramref name="position"/>.
		/// </summary>
		public void PlayFloatingText(string text, Vector3 position)
		{
			var floatingText = _floatingTextPool.Spawn();

			floatingText.transform.position = position;
			floatingText.SetText(text);

			this.LateCall(floatingText.AnimationLength, () => _floatingTextPool.Despawn(floatingText));
		}
	}
}