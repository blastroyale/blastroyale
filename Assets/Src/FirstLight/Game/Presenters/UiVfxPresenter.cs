using DG.Tweening;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using FirstLight.Services;
using FirstLight.UiService;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// /// <summary>
	/// This Presenter handles the VFX for the UI by:
	/// - Player new VFX animations
	/// </summary>
	public class UiVfxPresenter : UiPresenter
	{
		[SerializeField] private Image _imageRef;
		[SerializeField] private DOTweenAnimation _moveAnimation;
		[SerializeField] private DOTweenAnimation _scaleAnimation;
		[SerializeField] private Transform _defaultFloatingTextTransform;
		[SerializeField] private MainMenuFloatingTextView _floatingTextRef;

		private IObjectPool<Image> _imagePool;
		private IObjectPool<MainMenuFloatingTextView> _floatingTextPool;

		private void Start()
		{
			_imagePool = new GameObjectPool<Image>(10, _imageRef);
			_floatingTextPool = new GameObjectPool<MainMenuFloatingTextView>(10, _floatingTextRef);
			_moveAnimation.targetIsSelf = _scaleAnimation.targetIsSelf = false;
			_moveAnimation.forcedTargetType = DOTweenAnimation.TargetType.Transform;
			_moveAnimation.hasOnComplete = true;
		}

		/// <summary>
		/// Plays the VFX animation with the necessary information to play the moving animation to the given <paramref name="targetWorldPosition"/>
		/// It will execute the given <paramref name="onCompleteCallback"/> when the VFX ends 
		/// </summary>
		public void PlayAnimation(Sprite sprite, Vector3 originWorldPosition, Vector3 targetWorldPosition,
		                          UnityAction onCompleteCallback)
		{
			var image = _imagePool.Spawn();
			var imageTransform = image.transform;
			var closure = onCompleteCallback;

			image.sprite = sprite;
			imageTransform.position = originWorldPosition;

			_moveAnimation.useTargetAsV3 = false;
			_moveAnimation.target = imageTransform;
			_moveAnimation.endValueV3 = targetWorldPosition;
			_moveAnimation.targetGO = _scaleAnimation.targetGO = image.gameObject;
			
			_moveAnimation.CreateTween();
			_scaleAnimation.CreateTween();

			_moveAnimation.tween.OnComplete(OnCompleteTween);
			_moveAnimation.tween.Play();
			_scaleAnimation.tween.Play();

			void OnCompleteTween()
			{
				closure?.Invoke();
				_imagePool.Despawn(image);
			}
		}

		/// <summary>
		/// Plays the VFX animation with the necessary information to play the moving animation to the given <paramref name="target"/>
		/// It will execute the given <paramref name="onCompleteCallback"/> when the VFX ends 
		/// </summary>
		public void PlayAnimation(Sprite sprite, Vector3 originWorldPosition, Transform target,
		                          UnityAction onCompleteCallback)
		{
			var image = _imagePool.Spawn();
			var imageTransform = image.transform;
			var closure = onCompleteCallback;

			image.sprite = sprite;
			imageTransform.position = originWorldPosition;

			_moveAnimation.useTargetAsV3 = true;
			_moveAnimation.target = imageTransform;
			_moveAnimation.endValueTransform = target;
			_moveAnimation.targetGO = _scaleAnimation.targetGO = image.gameObject;
			
			_moveAnimation.CreateTween();
			_scaleAnimation.CreateTween();

			_moveAnimation.tween.OnComplete(OnCompleteTween);
			_moveAnimation.tween.Play();
			_scaleAnimation.tween.Play();

			void OnCompleteTween()
			{
				closure?.Invoke();
				_imagePool.Despawn(image);
			}
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