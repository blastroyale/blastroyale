using System;
using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Presenters.Manipulators;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class HudCustomizationScreenPresenter : UiToolkitPresenterData<HudCustomizationScreenPresenter.StateData>
	{
		private readonly float MIN_OPACITY = 0.05f;
		private readonly string USS_CUSTOMIZABLE_ELEMENT = "customizable-hud";
		private readonly string USS_NON_CUSTOMIZABLE_ELEMENT = "non-customizable-hud";
		private readonly string USS_CUSTOMIZING_CLASS = "customizing-element";
		
		private List<SerializedVisualElementSetup> _originals = new();
		private HashSet<VisualElement> _customizable = new ();
		private Vector2 _lastPos;
		private VisualElement _selected;
		private IGameServices _services;
		private Slider _opacity;
		private ImageButton _close;
		private ImageButton _save;
		private ImageButton _reset;
		
		public struct StateData
		{
			public Action<IReadOnlyCollection<VisualElement>> OnSave;
			public Action OnClose;
		}

		protected override void QueryElements(VisualElement root)
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_opacity = root.Q<Slider>("OpacitySlider").Required();
			_opacity.visible = false;
			_opacity.RegisterValueChangedCallback(OnOpacityChange);
			
			_reset = root.Q<ImageButton>("ResetButton").Required();
			_save = root.Q<ImageButton>("SaveButton").Required();
			_close = root.Q<ImageButton>("CloseButton").Required();
			
			_close.clicked += Data.OnClose;
			_save.clicked += () => Data.OnSave(_customizable);
			_reset.clicked += Reset;

			root.Query(className: USS_NON_CUSTOMIZABLE_ELEMENT).Build().ForEach(Remove);
			root.Query(className:  USS_CUSTOMIZABLE_ELEMENT).Build().ForEach(MakeCustomizable);

			// Joysticks are snowflakes because they handled the events on parent
			// if we block events on parent to drag without moving them, the propagation stops
			// and we cannot drag them
			var joy1 = root.Q<JoystickElement>("MovementJoystick");
			var joy2 = root.Q<JoystickElement>("ShootingJoystick");
			joy1.RemoveListeners();
			joy2.RemoveListeners();
			MakeCustomizable(joy1, onlyInsideParent:true);
			MakeCustomizable(joy2, onlyInsideParent:true);
			
			_services.ControlsSetup.SetControlPositions(root);
		}

		private void Reset()
		{
			foreach (var o in _originals)
			{
				var e = Root.Q(o.ElementId);
				if (e == null) continue;
				o.ToElement(e);
			}
		}

		private void OnOpacityChange(ChangeEvent<float> ev)
		{
			if (_selected == null) return;
			_selected.style.opacity = Math.Max(ev.newValue, MIN_OPACITY);
		}

		private void MakeCustomizable(VisualElement e)
		{
			MakeCustomizable(e, false);
		}
		
		private void MakeCustomizable(VisualElement e, bool onlyInsideParent)
		{
			_originals.Add(new SerializedVisualElementSetup().FromElement(e));
			_customizable.Add(e);
			var manipulator = new DragDropManipulator();
			manipulator.OnlyInsideParent = onlyInsideParent;
			manipulator.OnStartDrag += OnStartDrag;
			e.AddManipulator(manipulator);
		}

		private void OnStartDrag(VisualElement element)
		{
			if(_selected != null) _selected.RemoveFromClassList(USS_CUSTOMIZING_CLASS);
			element.AddToClassList(USS_CUSTOMIZING_CLASS);
			_selected = element;
			_opacity.visible = true;
			_opacity.value = element.style.opacity.value == 0 ? 1 : element.style.opacity.value;
		}

		private void Remove(VisualElement e)
		{
			RemoveEvents(e);
			e.style.display = DisplayStyle.None;
		}

		private void RemoveEvents(VisualElement e)
		{
			e.RegisterCallback<PointerDownEvent>(CancelEvent, TrickleDown.TrickleDown);
			e.RegisterCallback<PointerUpEvent>(CancelEvent, TrickleDown.TrickleDown);
			e.RegisterCallback<PointerMoveEvent>(CancelEvent, TrickleDown.TrickleDown);
		}

		private void CancelEvent(EventBase e)
		{
			e.PreventDefault();
			e.StopImmediatePropagation();
		}
	}
}