using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ReSharper disable CheckNamespace

namespace FirstLight.UiService
{
	/// <summary>
	/// Custom extension of unity's ui button class that plays a set of legacy
	/// animations when selected and unselected.
	/// </summary>
	[RequireComponent(typeof(UnityEngine.Animation))]
	public class UiToggleButtonView : Toggle
	{
		public Transform Anchor;
		public Ease PressedEase = Ease.Linear;
		public float PressedDuration = 0.1f;
		public Vector3 PressedScale = new Vector3(0.9f, 0.9f, 1f);
		public GameObject ToggleOn;
		public GameObject ToggleOff;
		public AnimationClip ToggleOnPressedClip;
		public AnimationClip ToggleOffPressedClip;
		public List<Image> CustomTargetGraphics;
		
		[HideInInspector] public Animation Animation;
		[HideInInspector] public RectTransform RectTransform;
		
		private Coroutine _coroutine;
		protected IGameServices _gameService;
		
#if UNITY_EDITOR
		/// <inheritdoc />
		protected override void OnValidate()
		{
			RectTransform = RectTransform ? RectTransform : GetComponent<RectTransform>();
			Animation = Animation ? Animation : GetComponent<Animation>();

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
			
			onValueChanged.AddListener(OnValueChanged);
		}
		
		/// <inheritdoc />
		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (_coroutine != null)
			{
				_gameService?.CoroutineService.StopCoroutine(_coroutine);
				_coroutine = null;
			}
			onValueChanged?.RemoveListener(OnValueChanged);
		}
		
		public void SetInitialValue(bool valueOn)
		{
			SetIsOnWithoutNotify(valueOn);
			ToggleOff.SetActive(!valueOn);
			ToggleOn.SetActive(valueOn);
			Animation.clip = isOn ? ToggleOnPressedClip : ToggleOffPressedClip;
			Animation.Rewind(); 
			Animation.Play();
		}

		/// <inheritdoc />
		public override void OnPointerDown(PointerEventData eventData)
		{
			base.OnPointerDown(eventData);

			if (!IsInteractable())
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
		public override void OnPointerClick(PointerEventData eventData)
		{
			if (!IsInteractable())
			{
				return;
			}
			
			OnClick();
			base.OnPointerClick(eventData);
		}

		/// <inheritdoc />
		public override void OnPointerUp(PointerEventData eventData)
		{
			if (!IsInteractable())
			{
				return;
			}
			
			base.OnPointerUp(eventData);
			
			if (_coroutine != null)
			{
				_gameService.CoroutineService.StopCoroutine(_coroutine);
				_coroutine = null;
			}
			
			if (!RectTransformUtility.RectangleContainsScreenPoint(RectTransform, eventData.position))
			{
				_gameService.AudioFxService.PlayClip2D(AudioId.ButtonClickForward);
				
				_coroutine = _gameService.CoroutineService.StartCoroutine(ScaleAfterPointerEventCo(Vector3.one));
			}
		}

		/// <summary>
		/// Sets the color of all 'custom target graphics'
		/// </summary>
		public void SetTargetCustomGraphicsColor(Color newColor)
		{
			foreach (var customImage in CustomTargetGraphics)
			{
				customImage.color = newColor;
			}
		}

		/// <summary>
		///  Called when pointer click event occurs
		/// </summary>
		protected virtual void OnClick()
		{
			_gameService.AudioFxService.PlayClip2D(AudioId.ButtonClickForward);
			
			Animation.clip = !isOn ? ToggleOnPressedClip : ToggleOffPressedClip;
			Animation.Rewind(); 
			Animation.Play();
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
		
		private void OnValueChanged(bool on)
		{
			ToggleOff.SetActive(!on);
			ToggleOn.SetActive(on);
		}
	}
}
