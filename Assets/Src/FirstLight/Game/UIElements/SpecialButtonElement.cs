using System;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.UIElements
{
	public class SpecialButtonElement : VisualElement
	{
		private const string USS_BLOCK = "special-button";
		private const string USS_DRAGGABLE = USS_BLOCK + "--draggable";
		private const string USS_PRESSED = USS_BLOCK + "--pressed";
		private const string USS_CONTAINER = USS_BLOCK + "__container";
		private const string USS_STICK = USS_BLOCK + "__stick";
		private const string USS_BG_CIRCLE = USS_BLOCK + "__bg-circle";
		private const string USS_ICON = USS_BLOCK + "__icon";
		private const string USS_COOLDOWN = USS_BLOCK + "__cooldown";
		private const string USS_CANCEL_CIRCLE = USS_BLOCK + "__cancel-circle";
		private const string USS_CANCEL_ICON = USS_BLOCK + "__cancel-icon";

		private const string USS_SPRITE_SPECIAL = "sprite-shared__icon-special-{0}";

		private readonly VisualElement _stick;
		private readonly VisualElement _container;
		private readonly VisualElement _icon;
		private readonly VisualElement _cooldown;
		private readonly Label _cooldownLabel;
		private readonly VisualElement _cancelCircle;
		private readonly VisualElement _cancelIcon;

		private Vector2 _startingPosition;

		private bool _draggable;
		private bool _onCooldown;
		private bool _inCancel;

		private IVisualElementScheduledItem _disableScheduledItem;

		/// <summary>
		/// Triggered with 0f when button is pressed and with 1f when button is released.
		/// </summary>
		public event Action<float> OnPress;
		
		/// <summary>
		/// Triggered with 0f when button is pressed and with 1f when button is released.
		/// </summary>
		public event Action<float> OnCancel;

		/// <summary>
		/// Triggered when the joystick is being dragged (always triggered between OnPress callbacks).
		/// </summary>
		public event Action<Vector2> OnDrag;

		public SpecialButtonElement()
		{
			AddToClassList(USS_BLOCK);
			pickingMode = PickingMode.Ignore;

			Add(_cancelCircle = new VisualElement {name = "cancel-circle"});
			_cancelCircle.AddToClassList(USS_CANCEL_CIRCLE);

			Add(_container = new VisualElement {name = "container"});
			_container.AddToClassList(USS_CONTAINER);

			var bgCircle = new VisualElement {name = "bg-circle"};
			_container.Add(bgCircle);
			bgCircle.AddToClassList(USS_BG_CIRCLE);

			_container.Add(_stick = new VisualElement {name = "stick"});
			_stick.AddToClassList(USS_STICK);

			_stick.Add(_icon = new VisualElement {name = "icon"});
			_icon.AddToClassList(USS_ICON);

			Add(_cancelIcon = new VisualElement {name = "cancel-icon"});
			_cancelIcon.AddToClassList(USS_CANCEL_ICON);

			_container.Add(_cooldown = new VisualElement {name = "cooldown"});
			_cooldown.AddToClassList(USS_COOLDOWN);
			_cooldown.SetVisibility(false);

			_cooldown.Add(_cooldownLabel = new Label("14") {name = "cooldown-label"});

			SetSpecial(GameId.SpecialAimingGrenade, true, 0);

			if (Application.isPlaying)
			{
				RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
				RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
			}
		}

		/// <summary>
		/// Sets the current special visuals and behaviour (if it's draggable or not).
		/// </summary>
		public void SetSpecial(GameId special, bool draggable, long availableIn)
		{
			if (special == GameId.TutorialGrenade)
			{
				special = GameId.SpecialAimingGrenade;
			}

			_draggable = draggable;
			_icon.RemoveSpriteClasses();
			_icon.AddToClassList(string.Format(USS_SPRITE_SPECIAL,
				special.ToString().ToLowerInvariant().Replace("special", "")));

			EnableInClassList(USS_DRAGGABLE, draggable);

			_disableScheduledItem?.Pause();
			if (availableIn > 0)
			{
				DisableFor(availableIn, null);
			}
			else if (_onCooldown)
			{
				DisableCooldown();
			}
		}

		/// <summary>
		/// Disables the special button for a given time with a visual countdown.
		/// </summary>
		public void DisableFor(long time, Action onEnable)
		{
			var seconds = Math.Max(1, (int) (time / 1000));

			_onCooldown = true;
			_cooldown.SetVisibility(true);
			_cooldownLabel.text = seconds.ToString();

			_disableScheduledItem = schedule.Execute(() =>
				{
					if (seconds > 0)
					{
						if (seconds <= 3)
						{
							_cooldownLabel.AnimatePing(1.2f);
						}

						_cooldownLabel.text = seconds.ToString();
					}
					else
					{
						DisableCooldown();
						onEnable?.Invoke();
					}

					seconds--;
				})
				.Every(1000)
				.Until(() => seconds < 0);
		}

		private void DisableCooldown()
		{
			_cooldownLabel.text = string.Empty;
			_onCooldown = false;
			_cooldown.SetVisibility(false);
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
			if (_onCooldown) return;

			_container.CapturePointer(evt.pointerId);

			AddToClassList(USS_PRESSED);

			var parentPosition = parent.WorldToLocal(evt.position);
			_startingPosition = parentPosition - new Vector2(worldBound.width / 2f, worldBound.height / 2f);

			OnPress?.Invoke(1f);
		}

		private void OnPointerMove(PointerMoveEvent evt)
		{
			if (_onCooldown || !_draggable || !_container.HasPointerCapture(evt.pointerId)) return;

			var parentPosition = parent.WorldToLocal(evt.position);
			var offsetPosition = parentPosition - new Vector2(worldBound.width / 2f, worldBound.height / 2f);

			var stickPosition = offsetPosition - _startingPosition;
			var stickPositionClamped = Vector2.ClampMagnitude(stickPosition, worldBound.width / 2f);
			var stickPositionClampedNormalized = stickPositionClamped / (worldBound.width / 2f);

			_stick.transform.position = stickPositionClamped;

			var inCancelArea = !_cancelCircle.ContainsPoint(_cancelCircle.WorldToLocal(evt.position));

			if (inCancelArea != _inCancel)
			{
				_inCancel = inCancelArea;
				_cancelIcon.SetVisibility(_inCancel);
				if (_inCancel)
				{
					// TODO: Maybe cancel the previous animation if it looks weird when quickly cycling
					_cancelIcon.AnimatePing(1.2f);
				}
			}

			stickPositionClampedNormalized.y = -stickPositionClampedNormalized.y;

			OnDrag?.Invoke(stickPositionClampedNormalized);
		}

		private void OnPointerUp(PointerUpEvent evt)
		{
			if (_onCooldown) return;

			_container.ReleasePointer(evt.pointerId);

			RemoveFromClassList(USS_PRESSED);

			_stick.transform.position = Vector3.zero;

			if (_inCancel)
			{
				_cancelIcon.SetVisibility(false);
				OnCancel?.Invoke(0f);
			}
			else
			{
				OnPress?.Invoke(0f);
			}
		}

		public new class UxmlFactory : UxmlFactory<SpecialButtonElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
		}
	}
}