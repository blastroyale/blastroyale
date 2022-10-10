using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// Handles / animates a single animatable UI VFX floating image.
	/// </summary>
	public class UiVfxImage : MonoBehaviour
	{
		[SerializeField, Required] private Image _image;
		[SerializeField, Required] private DOTweenAnimation _moveAnimation;
		[SerializeField, Required] private DOTweenAnimation _scaleAnimation;

		private void Awake()
		{
			_moveAnimation.targetIsSelf = _scaleAnimation.targetIsSelf = false;
			_moveAnimation.forcedTargetType = DOTweenAnimation.TargetType.Transform;
			_moveAnimation.hasOnComplete = true;
		}

		public void Play(Sprite sprite, float delay, Vector3 originWorldPosition, Vector3 targetWorldPosition,
			Action onComplete)
		{
			_image.sprite = sprite;
			_image.enabled = false;

			var t = transform;
			t.position = originWorldPosition;
			t.localScale = Vector3.zero;

			_moveAnimation.useTargetAsV3 = false;
			_moveAnimation.target = t;
			_moveAnimation.endValueV3 = targetWorldPosition;
			_moveAnimation.targetGO = _scaleAnimation.targetGO = gameObject;
			_moveAnimation.delay = delay;

			_scaleAnimation.delay = _moveAnimation.delay = delay;

			_moveAnimation.CreateTween(true);
			_scaleAnimation.CreateTween(true);

			_moveAnimation.tween.OnComplete(() => { onComplete(); });
			_moveAnimation.tween.OnStart(() => { _image.enabled = true; });
			_moveAnimation.tween.Play();
			_scaleAnimation.tween.Play();
		}
	}
}