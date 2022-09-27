using FirstLight.Game.Input;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
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
		[SerializeField, Required] private RectTransform _rootAnchor;
		[SerializeField, Required] private RectTransform _joystick;
		[SerializeField, Required] private RectTransform _handleImage;
		[SerializeField, Required] private UnityInputScreenControl _onscreenJoystickDirectionAdapter;
		[SerializeField, Required] private UnityInputScreenControl _onscreenJoystickPointerDownAdapter;
		[SerializeField, Required] private bool _dynamicJoystickCompatible = true;
		
		private PointerEventData _pointerDownData;
		private Vector2 _defaultJoystickPos = Vector2.zero;
		private bool _allowDynamicRepositioning;
		private IGameDataProvider _dataProvider;
		
		private int? CurrentPointerId => _pointerDownData?.pointerId;
		private float _joystickRadius;
		private float _joystickCorrectionRadius => _joystickRadius * GameConstants.Controls.DYNAMIC_JOYSTICK_THRESHOLD_MULT;
		
		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_defaultJoystickPos = _joystick.anchoredPosition;
			_joystickRadius = ((_joystick.rect.size.x / 2f) * _joystick.localScale.x) *
			                  GameConstants.Controls.MOVEMENT_JOYSTICK_RADIUS_MULT;

			if (_dynamicJoystickCompatible)
			{
				_allowDynamicRepositioning = _dataProvider.AppDataProvider.UseDynamicJoystick;
			}
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
			
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootAnchor, _joystick.position,
			                                                        eventData.pressEventCamera, out var joystickPosition);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootAnchor, eventData.position,
			                                                        eventData.pressEventCamera, out var position);
			
			var delta = position - joystickPosition;
			var deltaMag = delta.magnitude;
			var deltaMagClamp = Vector2.ClampMagnitude(delta, _joystickRadius);
			var deltaMagNorm = deltaMagClamp / _joystickRadius;
			
			// Makes the joystick float towards drag position, if the player dragged very far from initial press pos (UX)
			if (_allowDynamicRepositioning && deltaMag > _joystickCorrectionRadius)
			{
				var correctionDirVector = delta.normalized * _joystickCorrectionRadius;
				var deltaCorrection = delta - correctionDirVector;
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