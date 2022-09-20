using System;
using System.Collections;
using DG.Tweening;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using MoreMountains.NiceVibrations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// Custom extension of unity's ui button class that plays a legacy
	/// animation when pressed. 
	/// </summary>
	[RequireComponent(typeof(UnityEngine.Animation))]
	public class UiButtonView : Button
	{
		// Legacy animation component 
		[HideInInspector, Required] public Animation Animation;
		[HideInInspector, Required] public RectTransform RectTransform;
		
		// Ease for select and unselect scale tween playback
		public Ease PressedEase = Ease.Linear;
		// Duration of scale tween animation
		public float PressedDuration = 0.1f;
		// Final scale of button when pressed
		public Vector3 PressedScale = new Vector3(0.95f, 0.95f, 1f);
		public HapticTypes HapticType = HapticTypes.None;
		public Transform Anchor;
		public AnimationClip ClickClip;

		public bool PlaySound = true;
		public bool IsForward = true;
		public AudioId OverrideClickSfxId = AudioId.None;

		private const AudioId BUTTON_CLICK_FORWARD_SFX = AudioId.ButtonClickForward;
		private const AudioId BUTTON_CLICK_BACKWAWRD_SFX = AudioId.ButtonClickBackward;
		
		private Coroutine _coroutine;
		private IGameServices _gameService;

		public bool CanAnimate => Anchor != null && Animation != null && ClickClip != null;
		
#if UNITY_EDITOR
		/// <inheritdoc />
		protected override void OnValidate()
		{
			RectTransform = RectTransform ? RectTransform : GetComponent<RectTransform>();
			Animation = Animation ? Animation : GetComponent<Animation>();

			if (CanAnimate)
			{
				Debug.Assert(Anchor != null, $"Anchor has not been referenced {gameObject.FullGameObjectPath()}");
				Debug.Assert(Animation.clip != null, $"Animation has not been referenced {gameObject.FullGameObjectPath()}");
			}
			
			base.OnValidate();
		}
#endif	
		/// <inheritdoc />
		protected override void Awake()
		{
			base.Awake();
#if UNITY_EDITOR
			if (Application.isPlaying)
#endif
			{
				_gameService = MainInstaller.Resolve<IGameServices>();
			}
		}

		protected override void OnDisable()
		{
			gameObject.transform.localScale = Vector3.one;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (!CanAnimate)
			{
				return;
			}

			if (_coroutine != null)
			{
				_gameService?.CoroutineService.StopCoroutine(_coroutine);
				_coroutine = null;
			}
		}
		
		/// <inheritdoc />
		public override void OnPointerDown (PointerEventData eventData)
		{
			if (!CanAnimate || !IsInteractable())
			{
				return;
			}

			if (Animation.isPlaying)
			{
				Animation.Stop();
			}
			
			if (_coroutine != null)
			{
				_gameService.CoroutineService.StopCoroutine(_coroutine);
				_coroutine = null;
			}
			
			_coroutine = _gameService.CoroutineService.StartCoroutine(ScaleAfterPointerEventCo(PressedScale));
		}

		/// <inheritdoc />
		public override void OnPointerUp(PointerEventData eventData)
		{
			if (!CanAnimate || !IsInteractable())
			{
				return;
			}

			if (_coroutine != null)
			{
				_gameService.CoroutineService.StopCoroutine(_coroutine);
				_coroutine = null;
			}
			
			if (RectTransformUtility.RectangleContainsScreenPoint(RectTransform, eventData.position))
			{
				if (OverrideClickSfxId != AudioId.None && PlaySound)
				{
					_gameService.AudioFxService.PlayClip2D(OverrideClickSfxId);
				}
				else if (PlaySound)
				{
					_gameService.AudioFxService.PlayClip2D(IsForward
						                                       ? BUTTON_CLICK_FORWARD_SFX
						                                       : BUTTON_CLICK_BACKWAWRD_SFX);
				}

				Anchor.localScale = Vector3.one;
				Animation.clip = ClickClip;
				Animation.Rewind();
				Animation.Play();
			}
			else
			{
				_coroutine = _gameService.CoroutineService.StartCoroutine(ScaleAfterPointerEventCo(Vector3.one));
			}
		}

		/// <summary>
		/// Scale button to target scale over duration time
		/// </summary>
		private IEnumerator ScaleAfterPointerEventCo(Vector3 targetScale)
		{
			var endTime = Time.time + PressedDuration;
			var fromScale = Anchor.localScale;

			while (Time.time < endTime)
			{
				yield return null;
				UpdateScaleAfterPointerEvent(endTime, fromScale, targetScale, PressedEase);
			}
			
			Anchor.localScale = targetScale;
			_coroutine = null;
		}
		
		private void UpdateScaleAfterPointerEvent(float endTime, Vector3 fromScale, Vector3 targetScale, Ease ease)
		{
			var deltaTime = endTime - Time.time;
			var t = 1f - (deltaTime / PressedDuration);
			var localScale = Anchor.localScale;
			
			localScale.x = DOVirtual.EasedValue(fromScale.x, targetScale.x, t, ease);
			localScale.y = DOVirtual.EasedValue(fromScale.y, targetScale.y, t, ease);
			Anchor.localScale = localScale;
		}
	}
}