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
	public class SpecialButtonView : MonoBehaviour, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, 
	                                 IPointerDownHandler, IDragHandler
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
		[SerializeField, Required] private GameObject _normalAnchor;
		[SerializeField, Required] private GameObject _cancelAnchor;
		[SerializeField, Required] private UnityInputScreenControl _specialPointerDownAdapter;
		[SerializeField, Required] private UnityInputScreenControl _cancelPointerDownAdapter;
		[SerializeField, Required] private UnityInputScreenControl _specialAimDirectionAdapter;
		[SerializeField, Required] private RectTransform _rectTransform;
		[SerializeField] private Color _activeColor;
		[SerializeField] private Color _cooldownColor;
		[SerializeField] private float _rectScale = 1f;

		private IGameServices _services;
		private IAsyncCoroutine _cooldownCoroutine;
		private PointerEventData _pointerDownData;

		/// <summary>
		/// Request's the special <see cref="GameId"/> assigned to this special view button
		/// </summary>
		public GameId SpecialId { get; private set; }
		
		private int? CurrentPointerId => _pointerDownData?.pointerId;

		private void OnValidate()
		{
			_rectTransform ??= GetComponent<RectTransform>();
		}

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
			if (_pointerDownData != null)
			{
				return;
			}

			_pointerDownData = eventData;

			_specialAimDirectionAdapter.SendValueToControl(Vector2.zero);
			_specialPointerDownAdapter.SendValueToControl(1f);
		}

		/// <inheritdoc />
		public void OnPointerEnter(PointerEventData eventData)
		{
			if (CurrentPointerId != eventData.pointerId)
			{
				return;
			}
			
			_cancelPointerDownAdapter.SendValueToControl(1f);
			OnCancelEnter?.Invoke();
		}

		/// <inheritdoc />
		public void OnPointerExit(PointerEventData eventData)
		{
			if (CurrentPointerId != eventData.pointerId)
			{
				return;
			}

			if (_cancelAnchor.activeInHierarchy)
			{
				OnCancelExit?.Invoke();
			}
			
			_normalAnchor.SetActive(false);
			_cancelAnchor.SetActive(true);
		}

		/// <inheritdoc />
		public void OnDrag(PointerEventData eventData)
		{
			if (CurrentPointerId != eventData.pointerId)
			{
				return;
			}

			SetInputData(eventData);
		}

		/// <inheritdoc />
		public void OnPointerUp(PointerEventData eventData)
		{
			if (CurrentPointerId != eventData.pointerId)
			{
				return;
			}
			
			_pointerDownData = null;

			_normalAnchor.SetActive(true);
			_cancelAnchor.SetActive(false);
			SetInputData(eventData);
			
			if (RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, eventData.position, eventData.pressEventCamera))
			{
				_cancelPointerDownAdapter.SendValueToControl(0f);
				_specialPointerDownAdapter.SendValueToControl(0f);
			}
			else
			{
				_specialPointerDownAdapter.SendValueToControl(0f);
				_cancelPointerDownAdapter.SendValueToControl(0f);
			}
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
				return;
			}

			_specialIconImage.sprite =
				await _services.AssetResolverService.RequestAsset<SpecialType, Sprite>(specialConfig.SpecialType);
			_specialIconBackgroundImage.sprite =
				specialConfig.IsAimable ? _aimableBackgroundSprite : _nonAimableBackgroundSprite;
			_outerRingImage.enabled = specialConfig.IsAimable;
			
			_normalAnchor.SetActive(true);
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
			var end = Time.time + (special.AvailableTime - currentTime).AsFloat;
			var start = end - special.Cooldown.AsFloat;

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