using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays an interactive Joystick. Joystick pointer events are captured from it's parent.
	/// </summary>
	public class JoystickElement : VisualElement
	{
		private const string UssBlock = "joystick";
		private const string UssFree = UssBlock + "--free";
		private const string UssBgCircle = UssBlock + "__bg-circle";
		private const string UssStick = UssBlock + "__stick";
		private const string UssStickFree = UssStick + "--free";
		private const string UssDirectionHalo = UssBlock + "__direction-halo";

		private const string UssMatchButton = "match-button";
		private const string UssMatchButtonBgCircle = "match-button__bg-circle";

		private readonly VisualElement _stick;
		private readonly VisualElement _directionHalo;
		private Vector3 _initialPosition;
		
		public event Action<Vector2> OnMove;
		public event Action<float> OnClick; // Float so that we can use it directly with the input system

		public JoystickElement()
		{
			AddToClassList(UssBlock);
			AddToClassList(UssMatchButton);

			var bgCircle = new VisualElement {name = "bg-circle"};
			Add(bgCircle);
			bgCircle.AddToClassList(UssMatchButtonBgCircle);
			bgCircle.AddToClassList(UssBgCircle);

			Add(_directionHalo = new VisualElement {name = "direction-vfx"});
			_directionHalo.AddToClassList(UssDirectionHalo);
			_directionHalo.usageHints =
				UsageHints.DynamicTransform; // TODO: This could be added / removed in  PointerDown / PointerUp

			Add(_stick = new VisualElement {name = "stick"});
			_stick.AddToClassList(UssStick);
			_stick.usageHints = UsageHints.DynamicTransform;

			if (Application.isPlaying)
			{
				RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
				RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
			}
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			parent.RegisterCallback<PointerDownEvent>(OnPointerDown);
			parent.RegisterCallback<PointerMoveEvent>(OnPointerMove);
			parent.RegisterCallback<PointerUpEvent>(OnPointerUp);
		}

		private void OnDetachFromPanel(DetachFromPanelEvent evt)
		{
			RemoveListeners();
		}

		public void RemoveListeners()
		{
			parent.UnregisterCallback<PointerDownEvent>(OnPointerDown);
			parent.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
			parent.UnregisterCallback<PointerUpEvent>(OnPointerUp);
		}

		private void OnPointerDown(PointerDownEvent evt)
		{
			_initialPosition = transform.position;
			parent.CapturePointer(evt.pointerId);

			AddToClassList(UssFree);
			_stick.AddToClassList(UssStickFree);

			var parentPosition = parent.WorldToLocal(evt.position);
			var offsetPosition = parentPosition - new Vector2(worldBound.width / 2f, worldBound.height / 2f);

			transform.position = offsetPosition;

			OnClick?.Invoke(1f);
		}

		private void OnPointerMove(PointerMoveEvent evt)
		{
			if (!parent.HasPointerCapture(evt.pointerId)) return;

			var parentPosition = parent.WorldToLocal(evt.position);
			var offsetPosition = parentPosition - new Vector2(worldBound.width / 2f, worldBound.height / 2f);

			var stickPosition = offsetPosition - (Vector2) transform.position;
			var stickPositionClamped = Vector2.ClampMagnitude(stickPosition, worldBound.width / 2f);
			var stickPositionClampedNormalized = stickPositionClamped.normalized;

			_stick.transform.position = stickPositionClamped;
			_directionHalo.transform.rotation = VectorToRotation(stickPositionClampedNormalized);

			_directionHalo.style.opacity = stickPositionClamped.magnitude / (worldBound.width / 2f);

			stickPositionClampedNormalized.y = -stickPositionClampedNormalized.y;
			OnMove?.Invoke(stickPositionClampedNormalized);
		}

		private void OnPointerUp(PointerUpEvent evt)
		{
			parent.ReleasePointer(evt.pointerId);

			RemoveFromClassList(UssFree);
			_stick.RemoveFromClassList(UssStickFree);

			transform.position = _initialPosition;
			_stick.transform.position = Vector3.zero;
			_directionHalo.style.opacity = 0f;

			OnMove?.Invoke(Vector3.zero);
			OnClick?.Invoke(0f);
		}

		private static Quaternion VectorToRotation(Vector2 direction)
		{
			var angleInRadians = Mathf.Atan2(direction.y, direction.x);
			var angleInDegrees = angleInRadians * Mathf.Rad2Deg;

			// Normalize the angle to be between 0 and 360 degrees
			if (angleInDegrees < 0)
			{
				angleInDegrees += 360;
			}

			return Quaternion.Euler(0, 0, angleInDegrees + 90);
		}

		public new class UxmlFactory : UxmlFactory<JoystickElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
		}
	}
}