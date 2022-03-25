using FirstLight.Game.Input;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FirstLight.Game.Views.AdventureHudViews
{
	// TODO - delta - calculate actual delta, and ref to distToCenter

	/// <summary>
	/// Onscreen joystick class from Shoot & Loot.
	/// </summary>
	public class JoystickView : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
	{
		[SerializeField] private RectTransform[] _joysticks;
		[SerializeField] private Image _handleImage;
		[SerializeField] private UnityInputScreenControl _onscreenJoystickDirectionAdapter;
		[SerializeField] private UnityInputScreenControl _onscreenJoystickPointerDownAdapter;

		private PointerEventData _pointerDownData;

		private int? CurrentPointerId => _pointerDownData?.pointerId;
		private RectTransform MainJoystick => _joysticks[0];
		private Vector2 _defaultJoystickPos = Vector2.zero;

		private void Awake()
		{
			_defaultJoystickPos = MainJoystick.anchoredPosition;
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

			foreach (var joystick in _joysticks)
			{
				joystick.position = eventData.position;
			}

			_pointerDownData = eventData;
			_handleImage.rectTransform.anchoredPosition = Vector2.zero;

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

			var rectTransform = MainJoystick;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position,
			                                                        eventData.pressEventCamera, out var position);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.pressPosition,
			                                                        eventData.pressEventCamera, out var pressPosition);

			var radius = (rectTransform.rect.size.x / 2f) * rectTransform.localScale.x;
			var deltaFromCenter = Vector2.ClampMagnitude(position - pressPosition, radius);
			_handleImage.rectTransform.anchoredPosition = deltaFromCenter;
			_onscreenJoystickDirectionAdapter.SendValueToControl(deltaFromCenter / radius);
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
			foreach (var joystick in _joysticks)
			{
				joystick.anchoredPosition = _defaultJoystickPos;
			}

			_pointerDownData = null;
			_handleImage.rectTransform.anchoredPosition = Vector2.zero;

			_onscreenJoystickDirectionAdapter.SendValueToControl(Vector2.zero);
			_onscreenJoystickPointerDownAdapter.SendValueToControl(0f);
		}
	}
}