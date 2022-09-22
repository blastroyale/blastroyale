using FirstLight.Game.Input;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Onscreen view to control an UI joystick simulation
	/// </summary>
	public class JoystickView : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
	{
		[SerializeField] private RectTransform _rootAnchor;
		[SerializeField] private RectTransform _joystick;
		[SerializeField, Required] private RectTransform _handleImage;
		[SerializeField, Required] private UnityInputScreenControl _onscreenJoystickDirectionAdapter;
		[SerializeField, Required] private UnityInputScreenControl _onscreenJoystickPointerDownAdapter;
		[SerializeField] private float _radiusMultiplier = 1f;
		[SerializeField] private bool _allowFloating = true;
		
		private PointerEventData _pointerDownData;
		private Vector2 _defaultJoystickPos = Vector2.zero;
		
		private int? CurrentPointerId => _pointerDownData?.pointerId;
		private float _joystickRadius => ((_joystick.rect.size.x / 2f) * _joystick.localScale.x) * _radiusMultiplier;
		private float _joystickCorrectionRadius => _joystickRadius * 2f;
		
		private void Awake()
		{
			_defaultJoystickPos = _joystick.anchoredPosition;
		}
		
		private void OnEnable()
		{
			SetDefaultUI();
		}

		/// <inheritdoc />
		public void OnPointerDown(PointerEventData eventData)
		{
			if (CurrentPointerId.HasValue)
			{
				return;
			}
			
			_joystick.position = eventData.position;

			_pointerDownData = eventData;
			_handleImage.anchoredPosition = Vector2.zero;

			_onscreenJoystickDirectionAdapter.SendValueToControl(Vector2.zero);
			_onscreenJoystickPointerDownAdapter.SendValueToControl(1f);
		}

		/// <inheritdoc />
		public void OnDrag(PointerEventData eventData)
		{
			if (CurrentPointerId != eventData.pointerId)
			{
				return;
			}
			
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootAnchor, eventData.position,
			                                                        eventData.pressEventCamera, out var position);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootAnchor, eventData.pressPosition,
			                                                        eventData.pressEventCamera, out var pressPosition);
			
			var deltaMag = (position - _joystick.anchoredPosition).magnitude;
			var deltaMagClamp = Vector2.ClampMagnitude(position - pressPosition, _joystickRadius);
			var deltaMagNorm = deltaMagClamp / _joystickRadius;
			
			// Makes the joystick "float" towards drag position, if the player dragged very far from initial press pos (UX)
			if (deltaMag > _joystickCorrectionRadius)
			{
				var correctionDirVector = (position - _joystick.anchoredPosition).normalized * _joystickCorrectionRadius;
				var deltaCorrection = (position - _joystick.anchoredPosition) - correctionDirVector;
				_joystick.anchoredPosition += deltaCorrection;
			}
			
			_handleImage.anchoredPosition = deltaMagClamp;
			_onscreenJoystickDirectionAdapter.SendValueToControl(deltaMagNorm);
		}

		/// <inheritdoc />
		public void OnPointerUp(PointerEventData eventData)
		{
			if (CurrentPointerId != eventData.pointerId)
			{
				return;
			}

			SetDefaultUI();
		}

		private void SetDefaultUI()
		{
			_joystick.anchoredPosition = _defaultJoystickPos;

			_pointerDownData = null;
			_handleImage.anchoredPosition = Vector2.zero;

			_onscreenJoystickDirectionAdapter.SendValueToControl(Vector2.zero);
			_onscreenJoystickPointerDownAdapter.SendValueToControl(0f);
		}
	}
}