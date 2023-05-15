using System;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using MessagePack.Formatters;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class SpecialButtonElement : VisualElement
	{
		private const string UssBlock = "special-button";
		private const string UssDraggable = UssBlock + "--draggable";
		private const string UssPressed = UssBlock + "--pressed";
		private const string UssContainer = UssBlock + "__container";
		private const string UssStick = UssBlock + "__stick";
		private const string UssBgCircle = UssBlock + "__bg-circle";
		private const string UssIcon = UssBlock + "__icon";

		private const string UssSpriteSpecial = "sprite-shared__icon-special-{0}";

		private readonly VisualElement _stick;
		private readonly VisualElement _container;
		private readonly VisualElement _icon;

		private Vector2 _startingPosition;

		private bool _draggable;

		/// <summary>
		/// Triggered with 0f when button is pressed and with 1f when button is released.
		/// </summary>
		public event Action<float> OnPress;

		/// <summary>
		/// Triggered when the joystick is being dragged (always triggered between OnPress callbacks).
		/// </summary>
		public event Action<Vector2> OnDrag;

		public SpecialButtonElement()
		{
			AddToClassList(UssBlock);
			pickingMode = PickingMode.Ignore;

			Add(_container = new VisualElement {name = "container"});
			_container.AddToClassList(UssContainer);

			var bgCircle = new VisualElement {name = "bg-circle"};
			_container.Add(bgCircle);
			bgCircle.AddToClassList(UssBgCircle);

			_container.Add(_stick = new VisualElement {name = "stick"});
			_stick.AddToClassList(UssStick);

			_stick.Add(_icon = new VisualElement {name = "icon"});
			_icon.AddToClassList(UssIcon);

			SetSpecial(GameId.SpecialAimingGrenade, true);

			if (Application.isPlaying)
			{
				RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
				RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
			}
		}

		/// <summary>
		/// Sets the current special visuals and behaviour (if it's draggable or not).
		/// </summary>
		public void SetSpecial(GameId special, bool draggable)
		{
			_draggable = draggable;
			_icon.RemoveSpriteClasses();
			_icon.AddToClassList(string.Format(UssSpriteSpecial,
				special.ToString().ToLowerInvariant().Replace("special", "")));

			EnableInClassList(UssDraggable, draggable);
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			_container.RegisterCallback<PointerDownEvent>(OnPointerDown);
			_container.RegisterCallback<PointerMoveEvent>(OnPointerMove);
			_container.RegisterCallback<PointerUpEvent>(OnPointerUp);
		}

		private void OnDetachFromPanel(DetachFromPanelEvent evt)
		{
			_container.UnregisterCallback<PointerDownEvent>(OnPointerDown);
			_container.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
			_container.UnregisterCallback<PointerUpEvent>(OnPointerUp);
		}

		private void OnPointerDown(PointerDownEvent evt)
		{
			_container.CapturePointer(evt.pointerId);

			AddToClassList(UssPressed);

			var panelPosition = RuntimePanelUtils.ScreenToPanel(panel, evt.position);
			var parentPosition = parent.WorldToLocal(panelPosition);
			_startingPosition = parentPosition - new Vector2(worldBound.width / 2f, worldBound.height / 2f);

			OnPress?.Invoke(0f);
		}

		private void OnPointerMove(PointerMoveEvent evt)
		{
			if (!_draggable || !_container.HasPointerCapture(evt.pointerId)) return;

			var panelPosition = RuntimePanelUtils.ScreenToPanel(panel, evt.position);
			var parentPosition = parent.WorldToLocal(panelPosition);
			var offsetPosition = parentPosition - new Vector2(worldBound.width / 2f, worldBound.height / 2f);

			var stickPosition = offsetPosition - _startingPosition;
			var stickPositionClamped = Vector2.ClampMagnitude(stickPosition, worldBound.width / 2f);
			var stickPositionClampedNormalized = stickPositionClamped.normalized;

			_stick.transform.position = stickPositionClamped;

			stickPositionClampedNormalized.y = -stickPositionClampedNormalized.y;

			OnDrag?.Invoke(stickPositionClampedNormalized);
		}

		private void OnPointerUp(PointerUpEvent evt)
		{
			_container.ReleasePointer(evt.pointerId);

			RemoveFromClassList(UssPressed);

			_stick.transform.position = Vector3.zero;

			OnPress?.Invoke(1f);
		}

		public new class UxmlFactory : UxmlFactory<SpecialButtonElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
		}
	}
}