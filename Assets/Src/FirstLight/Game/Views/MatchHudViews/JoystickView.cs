using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Input;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Onscreen view to control an UI joystick simulation
	/// </summary>
	public class JoystickView : MonoBehaviour
	{ 
		[SerializeField, Required] private Image _backgroundCircle;
		[SerializeField, Required] private RectTransform _rootAnchor;
		[SerializeField, Required] private RectTransform _joystick;
		[SerializeField, Required] private RectTransform _handleImage;
		[SerializeField, Required] private UnityInputScreenControl _onscreenJoystickDirectionAdapter;
		[SerializeField, Required] private UnityInputScreenControl _onscreenJoystickPointerDownAdapter;
		[SerializeField, Required] private bool _dynamicJoystickCompatible = true;
		[SerializeField, Required] private bool _leftSideJoystick;
		
		private Vector2 _defaultJoystickPos = Vector2.zero;
		private bool _allowDynamicRepositioning;
		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private EventSystem _eventSystem;
		private Vector2 _deadzone = new (10, 10);
		private bool _leftDeadzone = false;
		private float _joystickRadius;
		private float _joystickCorrectionRadius;
		private int? _touch = null;
		
		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_eventSystem = EventSystem.current;
			_defaultJoystickPos = _joystick.anchoredPosition;
			_joystickRadius = ((_joystick.rect.size.x / 2f) * _joystick.localScale.x) * GameConstants.Controls.MOVEMENT_JOYSTICK_RADIUS_MULT;
			_joystickCorrectionRadius = _joystickRadius * GameConstants.Controls.DYNAMIC_JOYSTICK_THRESHOLD_MULT;
			
			if (_dynamicJoystickCompatible)
			{
				_allowDynamicRepositioning = _dataProvider.AppDataProvider.UseDynamicJoystick;
			}
		}

		private void OnEnable()
		{
			SetDefaultUI();
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
		
		private bool IsValidStartPosition(Vector2 touchScreenPos)
		{
			var viewportX = touchScreenPos.x / Screen.width;

			return _leftSideJoystick && viewportX <= 0.5f || !_leftSideJoystick && viewportX > 0.5f;
		}

		private bool IsCollidingWithHitbox(Touch touch)
		{
			// Have to override touch screen position as the new input system does not play well with old EventSystem 
			// which uses the old input module. These calculations will be simplified with UITK match controls
			PointerEventData data = new PointerEventData(EventSystem.current);
			data.position = touch.screenPosition;
			
			var results = new List<RaycastResult>();
			_eventSystem.RaycastAll(data, results);

			return results.Count == 1 && results[0].gameObject == gameObject;
		}

		private void OnFingerDown(Finger f)
		{
			var distanceMagnitude = (f.screenPosition - (Vector2) _joystick.position).sqrMagnitude;

			if (!_touch.HasValue && !float.IsInfinity(distanceMagnitude) && IsCollidingWithHitbox(f.currentTouch) &&
				IsValidStartPosition(f.currentTouch.screenPosition))
			{
				_touch = f.currentTouch.touchId;
				SetAlpha(_backgroundCircle, 0.6f);
				SetAlpha(_handleImage.GetComponent<Image>(), 0.6f);
				if (_leftSideJoystick || !_dataProvider.AppDataProvider.AngleTapShoot)
				{
					_joystick.position = f.screenPosition;
					_handleImage.anchoredPosition = Vector2.zero;
					_onscreenJoystickDirectionAdapter.SendValueToControl(Vector2.zero);
					if (_leftSideJoystick)
					{
						_onscreenJoystickPointerDownAdapter.SendValueToControl(1f);
					}
				}
				else 
				{
					var delta = f.currentTouch.screenPosition - (Vector2) _joystick.position;
					var clampedMag = Vector2.ClampMagnitude(delta, _joystickRadius);
					var normalized = clampedMag / _joystickRadius;
			
					if (_allowDynamicRepositioning && delta.sqrMagnitude > _joystickCorrectionRadius * _joystickCorrectionRadius)
					{
						var closestPosition = _joystick.position + (Vector3)Vector2.ClampMagnitude(delta, _joystickRadius - _joystickCorrectionRadius);
						_joystick.position = closestPosition;
						_joystick.anchoredPosition += delta - normalized * _joystickCorrectionRadius;
					}
					_handleImage.anchoredPosition = clampedMag;
					_onscreenJoystickDirectionAdapter.SendValueToControl(normalized);
					_onscreenJoystickPointerDownAdapter.SendValueToControl(1f);
				}
			}
		}

		private void OnFingerMove(Finger f)
		{
			if (!_touch.HasValue || _touch.Value != f.currentTouch.touchId) return;

			var delta = f.currentTouch.screenPosition - (Vector2) _joystick.position;
			
			if (_leftSideJoystick || !FeatureFlags.AIM_DEADZONE || (!_leftDeadzone && (Math.Abs(delta.x) > _deadzone.x || Math.Abs(delta.y) > _deadzone.y)))
			{
				_onscreenJoystickPointerDownAdapter.SendValueToControl(1f);
				_leftDeadzone = true;
			}

			var clampedMag = Vector2.ClampMagnitude(delta, _joystickRadius);
			var normalized = clampedMag / _joystickRadius;
			
			if (_allowDynamicRepositioning && delta.sqrMagnitude > _joystickCorrectionRadius * _joystickCorrectionRadius)
			{
				_joystick.anchoredPosition += delta - normalized * _joystickCorrectionRadius;
			}
			
			_handleImage.anchoredPosition = clampedMag;
			_onscreenJoystickDirectionAdapter.SendValueToControl(normalized);
			
		}

		private void OnFingerUp(Finger f)
		{
			if (_touch.HasValue && _touch.Value == f.currentTouch.touchId)
			{
				SetAlpha(_handleImage.GetComponent<Image>(), 1f);
				SetAlpha(_backgroundCircle, 0.2f);
				_touch = null;
				_leftDeadzone = false;
				SetDefaultUI();
			}
		}

		private void SetAlpha(Image i, float alpha)
		{
			i.color = new Color(i.color.r, i.color.g, i.color.b, alpha);
		}

		private void SetDefaultUI()
		{
			_joystick.anchoredPosition = _defaultJoystickPos;
			_handleImage.anchoredPosition = Vector2.zero;
			_onscreenJoystickDirectionAdapter.SendValueToControl(Vector2.zero);
			_onscreenJoystickPointerDownAdapter.SendValueToControl(0f);
		}
	}
}