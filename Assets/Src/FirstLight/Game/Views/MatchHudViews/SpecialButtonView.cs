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
		[SerializeField, Required] private Sprite _aimableBackgroundSprite;
		[SerializeField, Required] private Sprite _nonAimableBackgroundSprite;
		[SerializeField, Required] private Animation _pingAnimation;
		[SerializeField, Required] private UnityInputScreenControl _specialPointerDownAdapter;
		[SerializeField, Required] private UnityInputScreenControl _specialAimDirectionAdapter;
		[SerializeField] private Color _activeColor;
		[SerializeField] private Color _cooldownColor;
		[SerializeField] private float _rectScale = 1f;

		private QuantumSpecialConfig _specialConfig;
		private IGameServices _services;
		private Coroutine _cooldownCoroutine;
		private bool _isAiming;

		private void Awake()
		{
			QuantumEvent.Subscribe<EventOnLocalPlayerSpecialUsed>(this, OnEventOnLocalPlayerSpecialUsed);
		}

		private void OnDestroy()
		{
			if (_cooldownCoroutine != null)
			{
				_services?.CoroutineService?.StopCoroutine(_cooldownCoroutine);
			}
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
		public async void Init(Frame f, Special special, bool hasCharge)
		{
			_services ??= MainInstaller.Resolve<IGameServices>();

			gameObject.SetActive(false);

			if (!_services.ConfigsProvider.TryGetConfig((int) special.SpecialId, out _specialConfig))
			{
				return;
			}

			_specialIconImage.sprite =
				await _services.AssetResolverService.RequestAsset<SpecialType, Sprite>(_specialConfig.SpecialType);
			_specialIconBackgroundImage.sprite =
				_specialConfig.IsAimable ? _aimableBackgroundSprite : _nonAimableBackgroundSprite;
			_outerRingImage.enabled = _specialConfig.IsAimable;

			gameObject.SetActive(hasCharge);

			if (_cooldownCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_cooldownCoroutine);
			}

			if (!hasCharge)
			{
				return;
			}
			
			_cooldownCoroutine = _services.CoroutineService.StartCoroutine(SpecialCooldown(f.Time, special));
		}

		private void OnEventOnLocalPlayerSpecialUsed(EventOnLocalPlayerSpecialUsed callback)
		{
			if (callback.SpecialIndex != _specialIndex)
			{
				return;
			}

			if (_cooldownCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_cooldownCoroutine);
			}

			var time = callback.Game.Frames.Predicted.Time;

			_cooldownCoroutine = _services.CoroutineService.StartCoroutine(SpecialCooldown(time, callback.Special));
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

		private IEnumerator SpecialCooldown(FP currentTime, Special special)
		{
			var start = Time.time + (special.AvailableTime - special.Cooldown - currentTime).AsFloat;
			var end = Time.time + (special.AvailableTime - currentTime).AsFloat;

			_specialIconImage.color = _cooldownColor;
			_specialIconBackgroundImage.color = _cooldownColor;
			_buttonView.interactable = false;

			while (Time.time < end)
			{
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