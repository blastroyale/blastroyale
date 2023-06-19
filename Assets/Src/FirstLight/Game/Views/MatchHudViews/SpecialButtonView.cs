using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Input;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Photon.Deterministic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This class handles the Special Move button. As long as the player has special charges and the special is not in
	/// cooldown mode, they can use their special move during gameplay.
	/// </summary>
	[Obsolete("Please use SpecialButtonElement instead")]
	public class SpecialButtonView : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IDragHandler
	{
		public UnityEvent OnCancelEnter;
		public UnityEvent OnCancelExit;

		[SerializeField, Required] private Image _backgroundRadius;
		[SerializeField, Required] private UiButtonView _buttonView;
		[SerializeField, Required] private Image _specialIconImage;
		[SerializeField, Required] private Image _specialIconBackgroundImage;
		[SerializeField, Required] private Image _outerRingImage;
		[SerializeField, Required] private Sprite _aimableBackgroundSprite;
		[SerializeField, Required] private Sprite _nonAimableBackgroundSprite;
		[SerializeField, Required] private Animation _pingAnimation;
		[SerializeField, Required] private RectTransform _rootAnchor;
		[SerializeField, Required] private RectTransform _targetingCenterAnchor;
		[SerializeField, Required] private GameObject _specialAnchor;
		[SerializeField, Required] private GameObject _cancelAnchor;
		[SerializeField, Required] private UnityInputScreenControl _specialPointerDownAdapter;
		[SerializeField, Required] private UnityInputScreenControl _cancelPointerDownAdapter;
		[SerializeField, Required] private UnityInputScreenControl _specialAimDirectionAdapter;
		[SerializeField] private Color _activeColor;
		[SerializeField] private Color _cooldownColor;
		
		private Vector2 _defaultHandlePosition;
		private float _firstCancelRadius;
		private float _specialRadius;
		private float _cancelRadius;
		private QuantumSpecialConfig _cfg;
		private IGameServices _services;
		private IAsyncCoroutine _cooldownCoroutine;
		private float _lastDragDeltaMagSqr;
		private DateTime _cooldownEnd;
		private bool _startedValidSpecialInput;
		private bool _canTriggerCancelEnter;
		private bool _canTriggerCancelExit;
		private bool _firstCancelExit;
		
		private void OnEnable()
		{
			Touch.onFingerDown += OnFingerDown;
			Touch.onFingerMove += OnFingerMove;
			Touch.onFingerUp += OnFingerUp;
		}

		private void OnDisable()
		{
			Touch.onFingerDown -= OnFingerDown;
			Touch.onFingerMove -= OnFingerMove;
			Touch.onFingerUp -= OnFingerUp;
		}
		
		/// <summary>
		/// Request's the special <see cref="GameId"/> assigned to this special view button
		/// </summary>
		public GameId SpecialId { get; private set; }

		private int? _currentTouch;

		private void OnDestroy()
		{
			if (_cooldownCoroutine?.Coroutine != null)
			{
				_services?.CoroutineService?.StopCoroutine(_cooldownCoroutine.Coroutine);
			}
		}
		
		public void OnPointerDown(PointerEventData eventData)
		{
			if(!FeatureFlags.SPECIAL_NEW_INPUT) OnDown(eventData.pointerId);
		}
		
		public void OnDrag(PointerEventData eventData)
		{
			if (FeatureFlags.SPECIAL_NEW_INPUT)
			{
				return;
			}
			var center = _defaultHandlePosition;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootAnchor, center,
				eventData.pressEventCamera, out var buttonPosition);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootAnchor, eventData.position,
				eventData.pressEventCamera, out var position);
			OnDrag(eventData.pointerId, position, buttonPosition);
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if(!FeatureFlags.SPECIAL_NEW_INPUT) OnUp(eventData.pointerId);
		}
		
		private bool IsCollidingWithHitbox(Touch touch)
		{
			var delta = touch.screenPosition - _defaultHandlePosition;
			var deltaMag = delta.magnitude;
			return deltaMag < _specialRadius / 2;
		}

		private void OnFingerDown(Finger f)
		{
			if (!FeatureFlags.SPECIAL_NEW_INPUT)
			{
				return;
			}
			if (!_currentTouch.HasValue && IsCollidingWithHitbox(f.currentTouch))
			{
				OnDown(f.currentTouch.touchId);
			}
		}

		private void OnDown(int touchId)
		{
			if (_currentTouch.HasValue || DateTime.Now < _cooldownEnd)
			{
				return;
			}
			
			_currentTouch = touchId;
			_startedValidSpecialInput = true;
			_canTriggerCancelEnter = false;
			_canTriggerCancelExit = false;
			_firstCancelExit = true;
			
			SetAlpha(_backgroundRadius, 1);

			_specialAimDirectionAdapter.SendValueToControl(Vector2.zero);
			_specialPointerDownAdapter.SendValueToControl(1f);
			
			if (!_cfg.IsAimable)
			{
				OnUp(touchId);
			}
		}

		private void OnFingerMove(Finger f)
		{
			if (!FeatureFlags.SPECIAL_NEW_INPUT)
			{
				return;
			}
			var center = _defaultHandlePosition;
			OnDrag(f.currentTouch.touchId, f.currentTouch.screenPosition, center);
		}

		private void OnFingerUp(Finger f)
		{
			if (!FeatureFlags.SPECIAL_NEW_INPUT)
			{
				return;
			}
			OnUp(f.currentTouch.touchId);
		}

		private void OnDrag(int touchId, Vector2 position, Vector2 buttonPosition)
		{
			if (_currentTouch != touchId || !_startedValidSpecialInput)
			{
				return;
			}

			var delta = position - buttonPosition;
			var deltaMag = delta.magnitude;
			var deltaMagClamp = Vector2.ClampMagnitude(delta, _specialRadius);
			var deltaMagNorm = deltaMagClamp / _specialRadius;

			bool moveHandle = false;
			
			// Exit special radius first time
			if (_firstCancelExit && deltaMag >= _firstCancelRadius)
			{
				_firstCancelExit = false;
				_canTriggerCancelEnter = true;
				
				SetAlpha(_specialIconImage, 0.2f);
				
				_cancelAnchor.SetActive(true);
			}
			// Exit cancel radius
			else if (_canTriggerCancelExit && deltaMag >= _cancelRadius)
			{
				_canTriggerCancelExit = false;
				_canTriggerCancelEnter = true;
			
				SetAlpha(_specialIconImage, 0.2f);
				
				_cancelAnchor.SetActive(true);
				OnCancelExit?.Invoke();
			}
			// Enter cancel radius
			else if (_canTriggerCancelEnter && deltaMag <= _cancelRadius)
			{
				_canTriggerCancelEnter = false;
				_canTriggerCancelExit = true;
				_cancelPointerDownAdapter.SendValueToControl(1f);
				OnCancelEnter?.Invoke();
			}
			else
			{

					var closestPosition = _defaultHandlePosition + Vector2.ClampMagnitude(delta, _specialRadius);
					_specialAnchor.transform.position = closestPosition;
				
				_specialAimDirectionAdapter.SendValueToControl(deltaMagNorm);
			}
			_lastDragDeltaMagSqr = deltaMag;
		}
		
		private void OnUp(int pointerId)
		{
			if (_currentTouch != pointerId || !_startedValidSpecialInput)
			{
				return;
			}

			SetAlpha(_backgroundRadius, 0);
			SetAlpha(_specialIconImage, 1);
			
			
			_currentTouch = null;

			_specialAnchor.transform.position = _defaultHandlePosition;
			_cancelAnchor.SetActive(false);

			_cancelPointerDownAdapter.SendValueToControl(0f);
			_specialPointerDownAdapter.SendValueToControl(0f);
		}

		/// <summary>
		/// Requests status on whether currently player is dragging over cancel button, or out of it
		/// </summary>
		public bool DraggingValidPosition()
		{
			return (!_firstCancelExit && _lastDragDeltaMagSqr > _cancelRadius) || (_firstCancelExit);
		}

		/// <summary>
		/// Initializes the special button with it's necessary data
		/// </summary>
		public async void Init(GameId specialId)
		{
			_services ??= MainInstaller.Resolve<IGameServices>();
			SpecialId = specialId;

			gameObject.SetActive(false);

			if (!_services.ConfigsProvider.TryGetConfig<QuantumSpecialConfig>((int) specialId, out var specialConfig))
			{
				gameObject.SetActive(false);
				return;
			}

			if (_defaultHandlePosition == Vector2.zero)
			{
				_defaultHandlePosition = _specialAnchor.transform.position;
			}
			_cooldownEnd = DateTime.Now;
			_specialIconImage.sprite =
				await _services.AssetResolverService.RequestAsset<SpecialType, Sprite>(specialConfig.SpecialType);
			_specialIconBackgroundImage.sprite =
				specialConfig.IsAimable ? _aimableBackgroundSprite : _nonAimableBackgroundSprite;
			_outerRingImage.enabled = specialConfig.IsAimable;
			_cfg = specialConfig;
			var specialRect = _backgroundRadius.GetComponent<RectTransform>();
			var cancelRect = _cancelAnchor.GetComponent<RectTransform>();

			_firstCancelRadius = ((specialRect.rect.size.x / 2f) * specialRect.localScale.x) *
				GameConstants.Controls.SPECIAL_BUTTON_FIRST_CANCEL_RADIUS_MULT;
			_specialRadius = ((specialRect.rect.size.x / 2f) * specialRect.localScale.x) *
				GameConstants.Controls.SPECIAL_BUTTON_MAX_RADIUS_MULT;
			_cancelRadius = ((cancelRect.rect.size.x / 2f) * cancelRect.localScale.x) *
				GameConstants.Controls.SPECIAL_BUTTON_CANCEL_RADIUS_MULT;
			_specialAnchor.SetActive(true);
			_cancelAnchor.SetActive(false);
		}

		/// <summary>
		/// Update special button view data
		/// </summary>
		public IAsyncCoroutine SpecialUpdate(FP currentTime, Special special)
		{
			gameObject.SetActive(special.Charges > 0);

			if (_currentTouch.HasValue)
			{
				OnUp(_currentTouch.Value);
			}
			
			if (_cooldownCoroutine?.Coroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_cooldownCoroutine.Coroutine);
			}

			if (special.Charges == 0)
			{
				return null;
			}

			_cooldownCoroutine = _services.CoroutineService.StartAsyncCoroutine(SpecialCooldown(currentTime, special));

			return _cooldownCoroutine;
		}
		
		private void SetAlpha(Image i, float alpha)
		{
			i.color = new Color(i.color.r, i.color.g, i.color.b, alpha);
		}

		private IEnumerator SpecialCooldown(FP currentTime, Special special)
		{
			var end = Time.time + (special.AvailableTime - currentTime).AsFloat;
			var start = end - special.Cooldown.AsFloat;

			_specialIconImage.color = _cooldownColor;
			_specialIconBackgroundImage.color = _cooldownColor;
			_buttonView.interactable = false;

			_cooldownEnd = DateTime.Now.AddSeconds((special.AvailableTime - currentTime).AsFloat);

			while (Time.time < end)
			{
				var fill = Mathf.InverseLerp(start, end, Time.time);

				_specialIconImage.fillAmount = fill;
				_specialIconBackgroundImage.fillAmount = fill;

				yield return null;
			}

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