using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Input;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This class handles the Special Move button. As long as the player has special charges and the special is not in
	/// cooldown mode, they can use their special move during gameplay.
	/// </summary>
	public class SpecialButtonView : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IDragHandler
	{
		[SerializeField, Required] private UiButtonView _buttonView;
		[SerializeField, Required] private int _specialIndex;
		[SerializeField, Required] private Image _specialIconImage;
		[SerializeField, Required] private Image _specialIconBackgroundImage;
		[SerializeField, Required] private Image _outerRingImage;
		[SerializeField] private Color _activeColor;
		[SerializeField] private Color _cooldownColor;
		[SerializeField, Required] private UnityInputScreenControl _specialPointerDownAdapter;
		[SerializeField, Required] private UnityInputScreenControl _specialAimDirectionAdapter;
		[SerializeField] private float _rectScale = 1f;
		[SerializeField, Required] private Sprite _aimableBackgroundSprite;
		[SerializeField, Required] private Sprite _nonAimableBackgroundSprite;
		[SerializeField, Required] private Animation _pingAnimation;
		
		private IGameServices _services;
		private Coroutine _cooldownCoroutine;
		private bool _isAiming;

		private void Awake()
		{
			QuantumEvent.Subscribe<EventOnLocalSpecialUsed>(this, OnEventOnLocalSpecialUsed);
			QuantumEvent.Subscribe<EventOnLocalSpecialAvailable>(this, HandleLocalSpecialAvailable);
		}

		private void OnDestroy()
		{
			QuantumEvent.UnsubscribeListener(this);
			StopAllCoroutines();
		}

		/// <inheritdoc />
		public void OnPointerDown(PointerEventData eventData)
		{
			if (!_buttonView.interactable)
			{
				return;
			}

			_buttonView.interactable = false;
			_isAiming = true;
			
			_specialAimDirectionAdapter.SendValueToControl(Vector2.zero);
			_specialPointerDownAdapter.SendValueToControl(1f);
		}

		/// <inheritdoc />
		public void OnDrag(PointerEventData eventData)
		{
			if (!_isAiming)
			{
				return;
			}
			
			SetInputData(eventData);
		}
		
		/// <inheritdoc />
		public void OnPointerUp(PointerEventData eventData)
		{
			if (!_isAiming)
			{
				return;
			}

			_isAiming = false;
			
			SetInputData(eventData);
			_specialPointerDownAdapter.SendValueToControl(0f);
		}

		/// <summary>
		/// Initializes the special button with it's necessary data
		/// </summary>
		public async void Init(GameId special, bool hasCharge, FP cooldownTime)
		{
			_services ??= MainInstaller.Resolve<IGameServices>();
			
			gameObject.SetActive(false);
			
			if(!_services.ConfigsProvider.TryGetConfig<QuantumSpecialConfig>((int) special, out var config))
			{
				return;
			}
			
			_specialIconImage.sprite = await _services.AssetResolverService.RequestAsset<SpecialType, Sprite>(config.SpecialType);
			_specialIconBackgroundImage.sprite = config.IsAimable ? _aimableBackgroundSprite : _nonAimableBackgroundSprite;
			_outerRingImage.enabled = config.IsAimable;
			_specialIconImage.fillAmount = 0f;
			_specialIconBackgroundImage.fillAmount = 0f;
			_buttonView.interactable = false;
			
			gameObject.SetActive(true);
			
			if (_cooldownCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_cooldownCoroutine);
				_cooldownCoroutine = null;
			}

			if (hasCharge)
			{
				_cooldownCoroutine = _services.CoroutineService.StartCoroutine(SpecialCooldown(FP._0, cooldownTime));
			}
		}

		private void OnEventOnLocalSpecialUsed(EventOnLocalSpecialUsed callback)
		{
			if (callback.SpecialIndex != _specialIndex)
			{
				return;
			}

			if (_cooldownCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_cooldownCoroutine);
				_cooldownCoroutine = null;
			}

			_specialIconImage.color = _cooldownColor;
			_specialIconImage.fillAmount = 0f;
			_specialIconBackgroundImage.color = _cooldownColor;
			_specialIconBackgroundImage.fillAmount = 0f;
			_buttonView.interactable = false;

			_cooldownCoroutine = _services.CoroutineService.StartCoroutine(SpecialCooldown(callback.StartTime, callback.EndTime));
		}

		private void HandleLocalSpecialAvailable(EventOnLocalSpecialAvailable callback)
		{
			if (callback.SpecialIndex != _specialIndex)
			{
				return;
			}
			
			if (_cooldownCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_cooldownCoroutine);
			}

			FillButton();
		}

		private void SetInputData(PointerEventData eventData)
		{
			var rectTransform = _specialIconBackgroundImage.rectTransform;
			
			RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position,
				eventData.pressEventCamera, out var position);
			
			var radius = (rectTransform.rect.size.x / 2f) * rectTransform.localScale.x * _rectScale;
			var delta = Vector2.ClampMagnitude(position, radius);
			
			_specialAimDirectionAdapter.SendValueToControl(delta / radius);
		}
		
		private IEnumerator SpecialCooldown(FP startFixed, FP endFixed)
		{
			var start = Time.time;
			var end = start + endFixed.AsFloat - startFixed.AsFloat;
			
			_specialIconImage.color = _cooldownColor;
			_specialIconBackgroundImage.color = _cooldownColor;
			
			while (Time.time < end)
			{
				if (this.IsDestroyed())
				{
					yield break;
				}

				var fill = Mathf.InverseLerp(start, end, Time.time);
				_specialIconImage.fillAmount = fill;
				_specialIconBackgroundImage.fillAmount = fill;
				
				yield return null;
			}

			FillButton();
		}

		private void FillButton()
		{
			_pingAnimation.Rewind();
			_pingAnimation.Play();
			
			_specialIconImage.color = _activeColor;
			_specialIconImage.fillAmount = 1f;
			_specialIconBackgroundImage.color = _activeColor;
			_specialIconBackgroundImage.fillAmount = 1f;
			_cooldownCoroutine = null;
			_buttonView.interactable = true;
		}
	}
}

