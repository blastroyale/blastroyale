using System;
using System.Collections;
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
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This class handles the Special Move button. As long as the player has special charges and the special is not in
	/// cooldown mode, they can use their special move during gameplay.
	/// </summary>
	public class SpecialButtonView : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IDragHandler
	{
		public UnityEvent OnCancelEnter;
		public UnityEvent OnCancelExit;

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
		[SerializeField] private float _rectScale = 1f;

		private float _specialRadius;
		private float _cancelRadius;

		private IGameServices _services;
		private IAsyncCoroutine _cooldownCoroutine;
		private PointerEventData _pointerDownData;
		private float _lastDragDeltaMagSqr;
		private DateTime _cooldownEnd;
		private bool _startedValidSpecialInput;
		private bool _canTriggerCancelEnter;
		private bool _canTriggerCancelExit;
		private bool _firstCancelExit;
		
		/// <summary>
		/// Request's the special <see cref="GameId"/> assigned to this special view button
		/// </summary>
		public GameId SpecialId { get; private set; }

		private int? CurrentPointerId => _pointerDownData?.pointerId;
		
		private void OnDestroy()
		{
			if (_cooldownCoroutine?.Coroutine != null)
			{
				_services?.CoroutineService?.StopCoroutine(_cooldownCoroutine.Coroutine);
			}
		}

		/// <inheritdoc />
		public void OnPointerDown(PointerEventData eventData)
		{
			if (_pointerDownData != null || DateTime.Now < _cooldownEnd)
			{
				return;
			}

			_pointerDownData = eventData;
			_startedValidSpecialInput = true;
			_canTriggerCancelEnter = false;
			_canTriggerCancelExit = false;
			_firstCancelExit = true;
			
			_specialAimDirectionAdapter.SendValueToControl(Vector2.zero);
			_specialPointerDownAdapter.SendValueToControl(1f);
		}

		/// <inheritdoc />
		public void OnDrag(PointerEventData eventData)
		{
			if (CurrentPointerId != eventData.pointerId || !_startedValidSpecialInput)
			{
				return;
			}

			RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootAnchor, _targetingCenterAnchor.position,
			                                                        eventData.pressEventCamera, out var buttonPosition);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootAnchor, eventData.position,
			                                                        eventData.pressEventCamera, out var position);

			var delta = position - buttonPosition;
			var deltaMag = delta.magnitude;
			var deltaMagClamp = Vector2.ClampMagnitude(delta, _specialRadius);
			var deltaMagNorm = deltaMagClamp / _specialRadius;

			// Exit special radius first time
			if (_firstCancelExit && deltaMag >= _specialRadius)
			{
				_firstCancelExit = false;
				_canTriggerCancelEnter = true;
				_specialAnchor.SetActive(false);
				_cancelAnchor.SetActive(true);
			}
			// Exit cancel radius
			else if (_canTriggerCancelExit && deltaMag >= _cancelRadius)
			{
				_canTriggerCancelExit = false;
				_canTriggerCancelEnter = true;
				_specialAnchor.SetActive(false);
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
				_specialAimDirectionAdapter.SendValueToControl(deltaMagNorm);
			}
			
			_lastDragDeltaMagSqr = deltaMag;
		}

		/// <inheritdoc />
		public void OnPointerUp(PointerEventData eventData)
		{
			if (CurrentPointerId != eventData.pointerId || !_startedValidSpecialInput)
			{
				return;
			}

			_pointerDownData = null;
			_startedValidSpecialInput = false;
			_canTriggerCancelEnter = false;
			_canTriggerCancelExit = false;
			_firstCancelExit = true;
			
			_specialAnchor.SetActive(true);
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

			_cooldownEnd = DateTime.Now;
			_specialIconImage.sprite = await _services.AssetResolverService.RequestAsset<SpecialType, Sprite>(specialConfig.SpecialType);
			_specialIconBackgroundImage.sprite = specialConfig.IsAimable ? _aimableBackgroundSprite : _nonAimableBackgroundSprite;
			_outerRingImage.enabled = specialConfig.IsAimable;
			
			var specialRect = _specialAnchor.GetComponent<RectTransform>();
			var cancelRect = _cancelAnchor.GetComponent<RectTransform>();
			
			_specialRadius = ((specialRect.rect.size.x / 2f) * specialRect.localScale.x) * GameConstants.Controls.SPECIAL_BUTTON_RADIUS_MULTIPLIER;
			_cancelRadius = ((cancelRect.rect.size.x / 2f) * cancelRect.localScale.x) * GameConstants.Controls.SPECIAL_CANCEL_RADIUS_MULTIPLIER;
			_specialAnchor.SetActive(true);
			_cancelAnchor.SetActive(false);
		}

		/// <summary>
		/// Update special button view data
		/// </summary>
		public IAsyncCoroutine SpecialUpdate(FP currentTime, Special special)
		{
			gameObject.SetActive(special.Charges > 0);

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